using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizeWeapon;

public class CompProperties_DynamicTraits : CompProperties {
    public List<WeaponTraitDef> defaultWeaponTraitDefs = new();
    public List<Part> supportParts = new();

    public CompProperties_DynamicTraits() => compClass = typeof(CompDynamicTraits);
}