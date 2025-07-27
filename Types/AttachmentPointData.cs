using UnityEngine;
using Verse;

namespace CustomizeWeapon;

public class AttachmentPointData {
    public Part part;
    public TextureSet baseTexture;
    public Vector2 offset;
    public int layer;
    public float scale = 1f;
    public bool receivesColor = false;

    public void ExposeData() {
        Scribe_Values.Look(ref part, "part");
        Scribe_Values.Look(ref offset, "offset");
        Scribe_Values.Look(ref layer, "layer", 0);
        Scribe_Values.Look(ref scale, "scale", 1f);
        Scribe_Values.Look(ref receivesColor, "receivesColor", false);
    }
}