using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizeWeapon;

public class CompProperties_DynamicTraits : CompProperties {
    public List<Part> supportParts = new();
    public List<WeaponTraitDef> defaultWeaponTraitDefs = new();

    public CompProperties_DynamicTraits() => compClass = typeof(CompDynamicTraits);
}