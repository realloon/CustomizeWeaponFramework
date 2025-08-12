using RimWorld;
using Verse;

namespace CWF;

public class TraitModuleExtension : DefModExtension {
    public WeaponTraitDef weaponTraitDef;
    public Part part;

    public List<ThingDef> requiredWeaponDefs;
    public List<string> requiredWeaponTags;
    public List<ThingDef> excludeWeaponDefs;
    public List<string> excludeWeaponTags;

    public List<GraphicCase> graphicCases;

    // public Rarity rarity; // enum
}