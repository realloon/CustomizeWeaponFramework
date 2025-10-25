using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace CWF;

[UsedImplicitly]
public class CompProperties_DynamicTraits : CompProperties {
    public readonly List<Part> supportParts = [];
    public readonly List<WeaponTraitDef> defaultWeaponTraitDefs = [];

    public CompProperties_DynamicTraits() => compClass = typeof(CompDynamicTraits);
}