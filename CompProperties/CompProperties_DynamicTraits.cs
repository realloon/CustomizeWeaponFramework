using RimWorld;
using Verse;

namespace CWF;

// ReSharper disable once ClassNeverInstantiated.Global
public class CompProperties_DynamicTraits : CompProperties {
    // ReSharper disable once CollectionNeverUpdated.Global
    public readonly List<Part> supportParts = [];
    public readonly List<WeaponTraitDef> defaultWeaponTraitDefs = [];

    public CompProperties_DynamicTraits() => compClass = typeof(CompDynamicTraits);
}