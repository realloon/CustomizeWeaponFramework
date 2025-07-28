using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CustomizeWeapon;

public class CompDynamicGraphic : ThingComp {
    private CompProperties_DynamicGraphic Props => (CompProperties_DynamicGraphic)props;

    private Graphic _cachedGraphic;
    private bool _isDirty = true;

    public void Notify_GraphicDirty() {
        _isDirty = true;
    }

    public Graphic GetDynamicGraphic() {
        if (!_isDirty && _cachedGraphic != null) return _cachedGraphic;

        _cachedGraphic = GenerateGraphic();
        _isDirty = false;

        return _cachedGraphic;
    }

    public override void Notify_ColorChanged() {
        base.Notify_ColorChanged();
        Notify_GraphicDirty();
    }

    // === Helper ===
    private Graphic GenerateGraphic() {
        Log.Message("[&CWF Dev] Perform multi-texture compositing and render it as Graphic_Single...");

        var originalGraphicData = parent.def.graphicData;
        if (originalGraphicData == null) return BaseContent.BadGraphic;

        // Get canvas size
        var canvasSizeReference = ContentFinder<Texture2D>.Get(originalGraphicData.texPath);
        if (canvasSizeReference == null) {
            Log.Warning(
                $"[CWF] Could not load canvas size reference texture at {originalGraphicData.texPath}. " +
                "Falling back to 512x512.");
            canvasSizeReference = new Texture2D(512, 512);
        }

        // Make canvas
        var renderTex = RenderTexture.GetTemporary(canvasSizeReference.width, canvasSizeReference.height, 0);
        RenderTexture.active = renderTex;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;

        // Collect all layers that need to be rendered
        var layersToDraw = new List<(Texture2D texture, Vector2 offset,
            float scale, int sortOrder, Color color, Texture2D maskTexture)>();
        var compDynamicTraits = parent.TryGetComp<CompDynamicTraits>();

        foreach (var point in Props.attachmentPoints) {
            var ext = compDynamicTraits?.GetInstalledTraitFor(point.part) is { } installedTrait
                ? CustomizeWeaponUtility.GetModuleDefFor(installedTrait)?.GetModExtension<TraitModuleExtension>()
                : null;

            var finalTextureSet = ext?.texture ?? point.baseTexture;

            if (finalTextureSet == null) continue;

            var finalOffset = point.offset;
            finalOffset += finalTextureSet.offset;

            var specificOffsetData = ext?.offsets?.FirstOrFallback(o => o.weaponDef == parent.def);
            if (specificOffsetData != null) {
                finalOffset = specificOffsetData.offset; // override
            }

            // module layers
            var baseSortOrder = point.layer * 10;

            // outline layers
            if (!string.IsNullOrEmpty(finalTextureSet.outlinePath)) {
                var outlineTex = ContentFinder<Texture2D>.Get(finalTextureSet.outlinePath, false);
                if (outlineTex != null) {
                    layersToDraw.Add((outlineTex, finalOffset, point.scale, baseSortOrder - 1000, Color.white, null));
                }
            }

            // add texture layer
            if (string.IsNullOrEmpty(finalTextureSet.texturePath)) continue;
            var mainTex = ContentFinder<Texture2D>.Get(finalTextureSet.texturePath);
            if (mainTex == null) continue;

            var color = point.receivesColor
                ? parent.TryGetComp<CompColorable>()?.ColorDef?.color ?? originalGraphicData.color
                : Color.white;

            var mask = point.receivesColor
                ? ContentFinder<Texture2D>.Get(
                    originalGraphicData.maskPath.NullOrEmpty()
                        ? originalGraphicData.texPath + "_m"
                        : originalGraphicData.maskPath, false)
                : null;

            layersToDraw.Add((mainTex, finalOffset, point.scale, baseSortOrder + 1, color, mask));
        }

        // Render all layers in a single pass
        RenderTexture.active = renderTex;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, renderTex.width, renderTex.height, 0);
        var sortedLayers = layersToDraw.OrderBy(l => l.sortOrder).ToList();
        foreach (var layer in sortedLayers) {
            var scaledWidth = layer.texture.width * layer.scale;
            var scaledHeight = layer.texture.height * layer.scale;
            var y = (renderTex.height - scaledHeight) - layer.offset.y;
            var destRect = new Rect(layer.offset.x, y, scaledWidth, scaledHeight);
            if (layer.color != Color.white) {
                var tempMaterial = new Material(originalGraphicData.shaderType.Shader) { color = layer.color };
                if (tempMaterial.HasProperty(ShaderPropertyIDs.ColorTwo)) {
                    tempMaterial.SetColor(ShaderPropertyIDs.ColorTwo, layer.color);
                }

                if (layer.maskTexture != null && tempMaterial.HasProperty(ShaderPropertyIDs.MaskTex)) {
                    tempMaterial.SetTexture(ShaderPropertyIDs.MaskTex, layer.maskTexture);
                }

                Graphics.DrawTexture(destRect, layer.texture, tempMaterial);
                Object.Destroy(tempMaterial);
            } else {
                Graphics.DrawTexture(destRect, layer.texture);
            }
        }

        GL.PopMatrix();
        RenderTexture.active = null;

        // package and output
        var finalBakedTexture = new Texture2D(renderTex.width, renderTex.height);
        RenderTexture.active = renderTex;
        finalBakedTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        finalBakedTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTex);
        var graphic = new Graphic_Single();
        var request = new GraphicRequest(
            typeof(Graphic_Single), finalBakedTexture, ShaderDatabase.Cutout,
            originalGraphicData.drawSize, Color.white, Color.white,
            originalGraphicData, 0, null, null
        );
        graphic.Init(request);
        return graphic;
    }
}