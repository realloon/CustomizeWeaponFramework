using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace CWF;

/// <summary>
/// A rule set class used within `TraitModuleExtension`. It defines conditional logic that allows a module to dynamically enable or disable other part slots on a weapon when installed, based on a `WeaponMatcher`.
/// </summary>
[UsedImplicitly]
public class ConditionalPartModifier {
    /// <summary>
    /// The condition for applying this modifier. The rule only runs if the weapon matches these criteria.
    /// </summary>
    [UsedImplicitly]
    public readonly WeaponMatcher? matcher;

    /// <summary>
    /// A list of part slots to enable on the weapon when this rule is met.
    /// </summary>
    [UsedImplicitly]
    public readonly List<PartDef> enablesParts = [];

    /// <summary>
    /// A list of part slots to disable on the weapon when this rule is met.
    /// </summary>
    [UsedImplicitly]
    public readonly List<PartDef> disablesParts = [];
}