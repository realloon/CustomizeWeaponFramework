using Verse;

namespace CWF;

/// <summary>
/// Properties class that enables a ThingDef to host the `CompAbilityProvider` component. This component is responsible for granting temporary abilities to a pawn when they equip the weapon.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CompProperties_AbilityProvider : CompProperties {
    public CompProperties_AbilityProvider() => compClass = typeof(CompAbilityProvider);
}