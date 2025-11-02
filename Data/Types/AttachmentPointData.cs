using JetBrains.Annotations;
using Verse;

namespace CWF;

[UsedImplicitly]
public class AttachmentPointData {
    // ReSharper disable once InconsistentNaming
    public Part part;

    // ReSharper disable once InconsistentNaming
    public ModuleGraphicData? baseTexture;

    // ReSharper disable once InconsistentNaming
    public int layer;

    // ReSharper disable once InconsistentNaming
    public bool receivesColor;

    public void ExposeData() {
        Scribe_Values.Look(ref part, "part");
        Scribe_Values.Look(ref baseTexture, "baseTexture");
        Scribe_Values.Look(ref layer, "layer");
        Scribe_Values.Look(ref receivesColor, "receivesColor");
    }
}