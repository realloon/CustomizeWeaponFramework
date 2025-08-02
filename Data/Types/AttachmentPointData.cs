using Verse;

namespace CustomizeWeapon;

public class AttachmentPointData {
    public Part part;
    public ModuleGraphicData baseTexture;
    public int layer;
    public bool receivesColor = false;

    public void ExposeData() {
        Scribe_Values.Look(ref part, "part");
        Scribe_Values.Look(ref baseTexture, "baseTexture");
        Scribe_Values.Look(ref layer, "layer", 0);
        Scribe_Values.Look(ref receivesColor, "receivesColor", false);
    }
}