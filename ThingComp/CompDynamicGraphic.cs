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
        Log.Message("[&CWF Dev] Graphic rendering...");

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
                    graphicToRender = CustomizeWeaponUtility.GetGraphicDataFor(installedTrait, parent);
                }

                graphicToRender ??= point.baseTexture;

                if (graphicToRender == null) continue;

                var finalOffset = graphicToRender.offset ?? point.baseTexture?.offset ?? Vector2.zero;
                var finalScale = graphicToRender.scale ?? point.baseTexture?.scale ?? 1f;
                var baseSortOrder = point.layer * 10;

                if (!string.IsNullOrEmpty(graphicToRender.outlinePath)) {
                    var outlineTex = ContentFinder<Texture2D>.Get(graphicToRender.outlinePath, false);
                    if (outlineTex != null) {
                        layersToDraw.Add((outlineTex, finalOffset, finalScale, baseSortOrder - 999, Color.white, null));
                    }
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
                Object.Destroy(tempMaterial);
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
}