using JetBrains.Annotations;
using RimWorld;
using Verse;


namespace CWF;

[UsedImplicitly]
public class ModuleExtension : DefModExtension {
    [UsedImplicitly]
    public readonly WeaponTraitDef weaponTraitDef = new();

    [UsedImplicitly]
    public Part part;

    [UsedImplicitly]
    public List<ConditionalPartModifier>? conditionalPartModifiers;

    [UsedImplicitly]
    public List<ThingDef>? requiredWeaponDefs;

    [UsedImplicitly]
    public List<string>? requiredWeaponTags;

    [UsedImplicitly]
    public List<ThingDef>? excludeWeaponDefs;

    [UsedImplicitly]
    public List<string>? excludeWeaponTags;

    [UsedImplicitly]
    public List<GraphicCase>? graphicCases;

    // public Rarity rarity; // enum

    public override IEnumerable<string> ConfigErrors() {
        if (weaponTraitDef.defName == Def.DefaultDefName) {
            yield return "Required field 'weaponTraitDef' is missing in XML.";
        }

        if (part == Part.None) {
            yield return "Required field 'part' is missing in XML.";
        }
    }
}

[UsedImplicitly]
public class GraphicCase {
    [UsedImplicitly]
    public readonly WeaponMatcher? matcher;

    [UsedImplicitly]
    public readonly ModuleGraphicData? graphicData;

    [UsedImplicitly]
    public readonly int priority;
}