using JetBrains.Annotations;
using RimWorld;
using Verse;

// ReSharper disable InconsistentNaming

namespace CWF;

/// <summary>
/// Defines the core XML properties for the `CompDynamicTraits` component. It specifies which `PartDef` slots a weapon supports (`supportParts`) and which traits (`defaultWeaponTraitDefs`) the weapon should have by default upon creation.
/// </summary>
[UsedImplicitly]
public class CompProperties_DynamicTraits : CompProperties {
    /// <summary>
    /// A list of all part slots this weapon can have modules attached to.
    /// </summary>
    public readonly List<PartDef> supportParts = [];

    /// <summary>
    /// A list of traits that are pre-installed on this weapon when it is generated.
    /// </summary>
    public readonly List<WeaponTraitDef> defaultWeaponTraitDefs = [];

    public CompProperties_DynamicTraits() => compClass = typeof(CompDynamicTraits);
}