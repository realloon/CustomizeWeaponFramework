using Verse;
using CWF.Extensions;
using JetBrains.Annotations;
using RimWorld;

// ReSharper disable InconsistentNaming

namespace CWF;

/// <summary>
/// A reusable data class that defines a set of criteria for matching a weapon. It checks if a given weapon's ThingDef is present in `weaponDefs` or if it has any of the specified `weaponTags`. Used in `ConditionalPartModifier` and `GraphicCase` to apply rules selectively.
/// </summary>
[UsedImplicitly]
public class WeaponMatcher {
    /// <summary>
    /// A list of specific weapon ThingDefs to match against.
    /// </summary>
    [UsedImplicitly]
    public List<ThingDef> weaponDefs = [];

    /// <summary>
    /// A list of weaponTags to match against.
    /// </summary>
    [UsedImplicitly]
    public List<string> weaponTags = [];

    /// <summary>
    /// A list of traitsDefs to match against.
    /// </summary>
    [UsedImplicitly]
    public List<WeaponTraitDef> traitsDef = [];

    public bool IsMatch(ThingWithComps weapon)
    {
        bool defMatches = weaponDefs.Contains(weapon.def);

        bool tagMatches = !weaponTags.NullOrEmpty() &&
                          !weapon.def.weaponTags.NullOrEmpty() &&
                          weaponTags.Any(tag => weapon.def.weaponTags.Contains(tag));

        var currentTraits = weapon.GetComp<CompDynamicTraits>()?.Traits;

        bool traitMatches = traitsDef.NullOrEmpty() ||
                            (!currentTraits.IsNullOrEmpty() &&
                             traitsDef.Any(trait => currentTraits.Contains(trait)));

        return traitMatches && (defMatches || tagMatches);
    }

    public bool IsMatch(ThingDef weaponDef) {
        if (weaponDefs.Contains(weaponDef)) return true;

        if (weaponTags.Empty() || weaponDef.weaponTags.NullOrEmpty()) return false;

        return weaponTags.Any(tag => weaponDef.weaponTags.Contains(tag));
    }
}