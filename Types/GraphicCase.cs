using Verse;

namespace CustomizeWeapon;

public class GraphicCase {
    public string label; // A label for debugging purposes in the XML
    public WeaponMatcher matcher;
    public ModuleGraphicData graphicData;
    public int priority = 0;
}