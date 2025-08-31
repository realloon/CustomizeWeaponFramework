using RimWorld;
using Verse;

namespace CWF;

public class TraitModuleExtension : DefModExtension {
    public WeaponTraitDef weaponTraitDef = new();
    public Part part;

    public List<ConditionalPartModifier>? conditionalPartModifiers;

    public List<ThingDef>? requiredWeaponDefs;
    public List<string>? requiredWeaponTags;
    public List<ThingDef>? excludeWeaponDefs;
    public List<string>? excludeWeaponTags;

    public List<GraphicCase>? graphicCases;

    // public Rarity rarity; // enum

    public override IEnumerable<string> ConfigErrors() {
        if (weaponTraitDef.defName == "UnnamedDef") {
            yield return "Required field 'weaponTraitDef' is missing in XML.";
        }

        if (part == Part.None) {
            yield return "Required field 'part' is missing in XML.";
        }
    }
}