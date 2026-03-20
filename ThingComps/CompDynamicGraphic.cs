using UnityEngine;
using RimWorld;
using Verse;
using CWF.Extensions;

namespace CWF;

public class CompDynamicGraphic : ThingComp {
    private CompProperties_DynamicGraphic Props => (CompProperties_DynamicGraphic)props;

    private Graphic? _cachedGraphic;
    private Texture2D? _cachedBakedTexture;

    private bool _isDirty = true;

    private CompDynamicTraits? _compDynamicTraits;

    public override void Initialize(CompProperties properties) {
        base.Initialize(properties);
        _compDynamicTraits = parent.TryGetComp<CompDynamicTraits>();
    }

    public Graphic GetDynamicGraphic() {
        if (!_isDirty && _cachedGraphic != null) return _cachedGraphic;

        _cachedGraphic = GenerateGraphic();
        _isDirty = false;

        return _cachedGraphic;
    }

    public Texture? GetUIIconTexture() {
        return GetDynamicGraphic().MatSingleFor(parent).mainTexture;
    }

    public void Notify_GraphicDirty() {
        _isDirty = true;
    }

    public override void Notify_ColorChanged() {
        base.Notify_ColorChanged();
        Notify_GraphicDirty();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap) {
        base.PostDestroy(mode, previousMap);
        ReleaseCachedTexture();
    }

    public override bool DontDrawParent() => true;

    public override void DrawAt(Vector3 drawLoc, bool flip = false) {
        if (parent.def.drawerType != DrawerType.RealtimeOnly && parent.Spawned) return;

        var rotation = flip ? parent.Rotation.Opposite : parent.Rotation;
        GetDynamicGraphic().Draw(drawLoc, rotation, parent);
    }

    public override void PostPrintOnto(SectionLayer layer) {
        if (parent.def.dontPrint) return;

        GetDynamicGraphic().Print(layer, parent, 0f);
    }

    /// <summary>
    /// Finds the most suitable graphic data for a given module trait when applied to a specific weapon.
    /// It resolves the graphic based on the matching rules and priority defined in the module's TraitModuleExtension.
    /// </summary>
    public ModuleGraphicData? GetGraphicDataFor(WeaponTraitDef traitDef) {
        if (!traitDef.TryGetModuleDef(out var moduleDef)) return null;

        var ext = moduleDef.GetModExtension<TraitModuleExtension>();
        if (ext?.graphicCases.IsNullOrEmpty() ?? true) return null;

        var matchingCases = ext.graphicCases
            .Where(c => c.matcher != null && c.graphicData != null && c.matcher.IsMatch(parent))
            .ToList();

        if (matchingCases.Empty()) return null;

        var bestCase = matchingCases.MaxBy(c => c.priority);
        return bestCase.graphicData;
    }

    // === Helper ===
    private Graphic GenerateGraphic() {
        var originalGraphicData = parent.def.graphicData;
        if (originalGraphicData == null) return BaseContent.BadGraphic;

        var sizeReference = ContentFinder<Texture2D>.Get(originalGraphicData.texPath, false);
        var renderWidth = sizeReference?.width ?? _cachedBakedTexture?.width ?? 512;
        var renderHeight = sizeReference?.height ?? _cachedBakedTexture?.height ?? 512;

        var renderTexture = RenderTexture.GetTemporary(renderWidth, renderHeight, 0, RenderTextureFormat.ARGB32);

        var layersToDraw = new List<(Texture2D texture, Vector2 offset, float scale, int sortOrder,
            Color color, Texture2D? maskTexture)>();

        if (_compDynamicTraits != null) {
            foreach (var point in Props.attachmentPoints) {
                if (point.part == null) continue;

                var installedTrait = _compDynamicTraits.GetInstalledTraitFor(point.part);
                ModuleGraphicData? graphicToRender = null;

                if (installedTrait != null) {
                    graphicToRender = GetGraphicDataFor(installedTrait);
                }

                graphicToRender ??= point.baseTexture;

                if (graphicToRender == null) continue;

                var finalOffset = graphicToRender.offset ?? point.baseTexture?.offset ?? Vector2.zero;
                var finalScale = graphicToRender.scale ?? point.baseTexture?.scale ?? 1f;
                var baseSortOrder = point.layer * 10;

                // outline
                var outlineTex = GetOutlineTexture(graphicToRender);
                if (outlineTex != null) {
                    layersToDraw.Add((outlineTex, finalOffset, finalScale, baseSortOrder - 999, Color.white, null));
                }

                // module
                if (graphicToRender.texturePath.IsNullOrEmpty()) continue;
                var moduleTexture = ContentFinder<Texture2D>.Get(graphicToRender.texturePath, false);
                if (moduleTexture == null) continue;

                var color = point.receivesColor
                    ? parent.TryGetComp<CompColorable>()?.ColorDef?.color ?? originalGraphicData.color
                    : Color.white;

                var mask = GetMaskTexture(originalGraphicData, point);

                layersToDraw.Add((moduleTexture, finalOffset, finalScale, baseSortOrder, color, mask));
            }
        }

        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);

        layersToDraw.Sort((left, right) => left.sortOrder.CompareTo(right.sortOrder));

        Material? tempMaterial = null;

        foreach (var layer in layersToDraw) {
            var scaledWidth = layer.texture.width * layer.scale;
            var scaledHeight = layer.texture.height * layer.scale;
            var y = (renderTexture.height - scaledHeight) - layer.offset.y;
            var destRect = new Rect(layer.offset.x, y, scaledWidth, scaledHeight);

            if (layer.color != Color.white && originalGraphicData.shaderType?.Shader != null) {
                tempMaterial ??= new Material(originalGraphicData.shaderType.Shader);
                tempMaterial.color = layer.color;

                if (tempMaterial.HasProperty(ShaderPropertyIDs.ColorTwo)) {
                    tempMaterial.SetColor(ShaderPropertyIDs.ColorTwo, layer.color);
                }

                if (tempMaterial.HasProperty(ShaderPropertyIDs.MaskTex)) {
                    tempMaterial.SetTexture(ShaderPropertyIDs.MaskTex, layer.maskTexture);
                }

                Graphics.DrawTexture(destRect, layer.texture, tempMaterial);
            } else {
                Graphics.DrawTexture(destRect, layer.texture);
            }
        }

        if (tempMaterial != null) {
            UnityEngine.Object.Destroy(tempMaterial);
        }

        GL.PopMatrix();

        var finalBakedTexture = EnsureBakedTexture(renderTexture.width, renderTexture.height, out var textureRecreated);
        Graphics.CopyTexture(renderTexture, finalBakedTexture);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        if (_cachedGraphic is Graphic_Single graphicSingle && !textureRecreated) {
            _cachedGraphic = WrapGraphicIfNeeded(graphicSingle, originalGraphicData);
            return _cachedGraphic;
        }

        graphicSingle = new Graphic_Single();
        var request = new GraphicRequest(
            typeof(Graphic_Single), finalBakedTexture, ShaderDatabase.Cutout,
            originalGraphicData.drawSize, Color.white, Color.white,
            originalGraphicData, 0, null, null
        );

        graphicSingle.Init(request);
        _cachedGraphic = WrapGraphicIfNeeded(graphicSingle, originalGraphicData);

        return _cachedGraphic;
    }

    private static Graphic WrapGraphicIfNeeded(Graphic graphic, GraphicData originalGraphicData) {
        if (originalGraphicData.onGroundRandomRotateAngle > 0.01f) {
            graphic = new Graphic_RandomRotated(graphic, originalGraphicData.onGroundRandomRotateAngle);
        }

        return graphic;
    }

    private Texture2D EnsureBakedTexture(int width, int height, out bool textureRecreated) {
        if (_cachedBakedTexture != null && _cachedBakedTexture.width == width && _cachedBakedTexture.height == height) {
            textureRecreated = false;
            return _cachedBakedTexture;
        }

        ReleaseCachedTexture();

        textureRecreated = true;
        _cachedBakedTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        return _cachedBakedTexture;
    }

    private void ReleaseCachedTexture() {
        if (_cachedBakedTexture == null) return;

        UnityEngine.Object.Destroy(_cachedBakedTexture);
        _cachedBakedTexture = null;
    }

    private static Texture2D? GetMaskTexture(GraphicData graphicData, AttachmentPointData point) {
        var maskPathToLoad = graphicData.maskPath.NullOrEmpty()
            ? graphicData.texPath + "_m"
            : graphicData.maskPath;

        return point.receivesColor
            ? ContentFinder<Texture2D>.Get(maskPathToLoad, false)
            : null;
    }

    public static Texture2D? GetOutlineTexture(ModuleGraphicData graphicData) {
        string? outlinePathToLoad = null;

        if (!graphicData.outlinePath.NullOrEmpty()) {
            outlinePathToLoad = graphicData.outlinePath;
        } else if (!graphicData.texturePath.NullOrEmpty()) {
            outlinePathToLoad = graphicData.texturePath + "_o";
        }

        return outlinePathToLoad != null ? ContentFinder<Texture2D>.Get(outlinePathToLoad, false) : null;
    }
}
