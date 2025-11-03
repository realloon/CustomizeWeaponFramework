using JetBrains.Annotations;
using Verse;

// ReSharper disable InconsistentNaming

namespace CWF;

[UsedImplicitly]
public class AttachmentPointData {
    public PartDef? part;
    public ModuleGraphicData? baseTexture;
    public int layer;
    public bool receivesColor;

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