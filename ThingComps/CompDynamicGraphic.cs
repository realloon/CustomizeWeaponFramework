using UnityEngine;
using RimWorld;
using Verse;

namespace CWF;

public class CompDynamicGraphic : ThingComp {
    private CompProperties_DynamicGraphic Props => (CompProperties_DynamicGraphic)props;

    private Graphic _cachedGraphic;

    private bool _isDirty = true;

    public Graphic GetDynamicGraphic() {
        if (!_isDirty && _cachedGraphic != null) return _cachedGraphic;

        _cachedGraphic = GenerateGraphic();
        _isDirty = false;

        return _cachedGraphic;
    }

    public void Notify_GraphicDirty() {
        _isDirty = true;
    }

    public override void Notify_ColorChanged() {
        base.Notify_ColorChanged();
        Notify_GraphicDirty();
    }

    /// <summary>
    /// Finds the most suitable graphic data for a given module trait when applied to a specific weapon.
    /// It resolves the graphic based on the matching rules and priority defined in the module's TraitModuleExtension.
    /// </summary>
    public ModuleGraphicData GetGraphicDataFor(WeaponTraitDef traitDef) {
        if (traitDef == null) return null;

        var moduleDef = TraitModuleDatabase.GetModuleDefFor(traitDef);
        if (moduleDef == null) return null;

        var ext = moduleDef.GetModExtension<TraitModuleExtension>();
        if (ext?.graphicCases.NullOrEmpty() ?? true) return null;

        var matchingCases = ext.graphicCases
            .Where(c => c.matcher != null && c.graphicData != null && c.matcher.IsMatch(parent.def))
            .ToList();

        if (!matchingCases.Any()) return null;

        var bestCase = matchingCases.MaxBy(c => c.priority);
        return bestCase.graphicData;
    }

    // === Helper ===
    private Graphic GenerateGraphic() {
#if DEBUG
        Log.Message("[CWF Dev] Graphic rendering...");
#endif

        var originalGraphicData = parent.def.graphicData;
        if (originalGraphicData == null) return BaseContent.BadGraphic;

        var sizeReference = ContentFinder<Texture2D>.Get(originalGraphicData.texPath, false)
                            ?? new Texture2D(512, 512);

        var renderTexture = RenderTexture.GetTemporary(sizeReference.width, sizeReference.height, 0);

        var layersToDraw = new List<(Texture2D texture, Vector2 offset, float scale, int sortOrder,
            Color color, Texture2D maskTexture)>();

        var compDynamicTraits = parent.TryGetComp<CompDynamicTraits>();
        if (compDynamicTraits != null) {
            foreach (var point in Props.attachmentPoints) {
                var installedTrait = compDynamicTraits.GetInstalledTraitFor(point.part);

                ModuleGraphicData graphicToRender = null;
                if (installedTrait != null) {
                    graphicToRender = GetGraphicDataFor(installedTrait);
                }

                graphicToRender ??= point.baseTexture;

                if (graphicToRender == null) continue;

                var finalOffset = graphicToRender.offset ?? point.baseTexture?.offset ?? Vector2.zero;
                var finalScale = graphicToRender.scale ?? point.baseTexture?.scale ?? 1f;
                var baseSortOrder = point.layer * 10;

                var outlineTex = GetOutlineTexture(graphicToRender);
                if (outlineTex != null) {
                    layersToDraw.Add((outlineTex, finalOffset, finalScale, baseSortOrder - 999, Color.white, null));
                }

                if (string.IsNullOrEmpty(graphicToRender.texturePath)) continue;
                var moduleTexture = ContentFinder<Texture2D>.Get(graphicToRender.texturePath);
                if (moduleTexture == null) continue;

                var color = point.receivesColor
                    ? parent.TryGetComp<CompColorable>()?.ColorDef?.color ?? originalGraphicData.color
                    : Color.white;

                var mask = point.receivesColor
                    ? ContentFinder<Texture2D>.Get(
                        originalGraphicData.maskPath.NullOrEmpty()
                            ? originalGraphicData.texPath + "_m"
                            : originalGraphicData.maskPath, false)
                    : null;

                layersToDraw.Add((moduleTexture, finalOffset, finalScale, baseSortOrder, color, mask));
            }
        }

        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);

        var sortedLayers = layersToDraw.OrderBy(l => l.sortOrder).ToList();

        foreach (var layer in sortedLayers) {
            var scaledWidth = layer.texture.width * layer.scale;
            var scaledHeight = layer.texture.height * layer.scale;
            var y = (renderTexture.height - scaledHeight) - layer.offset.y;
            var destRect = new Rect(layer.offset.x, y, scaledWidth, scaledHeight);

            if (layer.color != Color.white && originalGraphicData.shaderType?.Shader != null) {
                var tempMaterial = new Material(originalGraphicData.shaderType.Shader) { color = layer.color };
                if (tempMaterial.HasProperty(ShaderPropertyIDs.ColorTwo)) {
                    tempMaterial.SetColor(ShaderPropertyIDs.ColorTwo, layer.color);
                }

                if (layer.maskTexture != null && tempMaterial.HasProperty(ShaderPropertyIDs.MaskTex)) {
                    tempMaterial.SetTexture(ShaderPropertyIDs.MaskTex, layer.maskTexture);
                }

                Graphics.DrawTexture(destRect, layer.texture, tempMaterial);
                UnityEngine.Object.Destroy(tempMaterial);
            } else {
                Graphics.DrawTexture(destRect, layer.texture);
            }
        }

        GL.PopMatrix();

        var finalBakedTexture = new Texture2D(renderTexture.width, renderTexture.height);
        finalBakedTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        finalBakedTexture.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        var graphic = new Graphic_Single();
        var request = new GraphicRequest(
            typeof(Graphic_Single), finalBakedTexture, ShaderDatabase.Cutout,
            originalGraphicData.drawSize, Color.white, Color.white,
            originalGraphicData, 0, null, null
        );

        graphic.Init(request);
        return graphic;
    }

    public static Texture2D GetOutlineTexture(ModuleGraphicData graphicData) {
        if (graphicData == null) return null;

        string outlinePathToLoad = null;
        if (!string.IsNullOrEmpty(graphicData.outlinePath)) {
            outlinePathToLoad = graphicData.outlinePath;
        } else if (!string.IsNullOrEmpty(graphicData.texturePath)) {
            outlinePathToLoad = graphicData.texturePath + "_o";
        }

        return outlinePathToLoad != null ? ContentFinder<Texture2D>.Get(outlinePathToLoad, false) : null;
    }
}