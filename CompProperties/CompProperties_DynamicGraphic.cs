using JetBrains.Annotations;
using Verse;

// ReSharper disable InconsistentNaming

namespace CWF;

/// <summary>
/// XML properties for the `CompDynamicGraphic` component. This class defines the various `attachmentPoints` on a weapon, specifying where module graphics can be rendered. It is the core configuration for a weapon's visual customization.
/// </summary>
public class CompProperties_DynamicGraphic : CompProperties {
    /// <summary>
    /// A list of visual attachment points on the weapon where module graphics will be rendered.
    /// </summary>
    [UsedImplicitly]
    public readonly List<AttachmentPointData> attachmentPoints = [];

    public CompProperties_DynamicGraphic() => compClass = typeof(CompDynamicGraphic);
}

/// <summary>
/// A data container class used within `CompProperties_DynamicGraphic`. It defines a single visual attachment location on a weapon, linking a `PartDef` to its rendering properties like draw `layer`, `baseTexture`, and whether it `receivesColor`.
/// </summary>
[UsedImplicitly]
public class AttachmentPointData {
    /// <summary>
    /// The logical part slot this visual point corresponds to.
    /// </summary>
    public PartDef? part;

    /// <summary>
    /// The default graphic to display at this point if no module is installed in the corresponding part slot.
    /// </summary>
    public ModuleGraphicData? baseTexture;

    /// <summary>
    /// The draw order for this attachment point. Higher numbers are drawn on top.
    /// </summary>
    public int layer;

    /// <summary>
    /// If true, the graphic at this point will be tinted with the weapon's primary color from `CompColorable`.
    /// </summary>
    public bool receivesColor;

    [UsedImplicitly]
    public void ExposeData() {
        Scribe_Defs.Look(ref part, "part");

        if (Scribe.mode == LoadSaveMode.LoadingVars && part == null) { // old enum part
            var oldPart = Part.None;
            Scribe_Values.Look(ref oldPart, "part");

            if (oldPart != Part.None) {
                part = PartEnumConverter.Convert(oldPart);
            }
        }

        Scribe_Values.Look(ref baseTexture, "baseTexture");
        Scribe_Values.Look(ref layer, "layer");
        Scribe_Values.Look(ref receivesColor, "receivesColor");
    }
}