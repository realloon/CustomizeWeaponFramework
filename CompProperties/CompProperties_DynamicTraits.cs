using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace CWF;

[UsedImplicitly]
public class CompProperties_DynamicTraits : CompProperties {
    // ReSharper disable once InconsistentNaming
    public readonly List<Part> supportParts = [];

    // ReSharper disable once InconsistentNaming
    public readonly List<WeaponTraitDef> defaultWeaponTraitDefs = [];

    public CompProperties_DynamicTraits() => compClass = typeof(CompDynamicTraits);
}