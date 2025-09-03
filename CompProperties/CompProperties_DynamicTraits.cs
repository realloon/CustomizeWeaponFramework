using RimWorld;
using Verse;

namespace CWF;

// ReSharper disable once ClassNeverInstantiated.Global
public class CompProperties_DynamicTraits : CompProperties {
    // ReSharper disable once CollectionNeverUpdated.Global
    public readonly List<Part> supportParts = new();
    public readonly List<WeaponTraitDef> defaultWeaponTraitDefs = new();

    public CompProperties_DynamicTraits() => compClass = typeof(CompDynamicTraits);
}