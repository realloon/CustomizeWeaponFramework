using Verse;

namespace CWF;

// ReSharper disable once ClassNeverInstantiated.Global
public class AttachmentPointData {
    public Part part;
    public ModuleGraphicData? baseTexture;
    public int layer;
    public bool receivesColor;

    public void ExposeData() {
        Scribe_Values.Look(ref part, "part");
        Scribe_Values.Look(ref baseTexture, "baseTexture");
        Scribe_Values.Look(ref layer, "layer");
        Scribe_Values.Look(ref receivesColor, "receivesColor");
    }
}