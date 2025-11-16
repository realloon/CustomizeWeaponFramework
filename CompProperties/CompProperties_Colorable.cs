using Verse;

namespace CWF;

/// <summary>
/// Properties class that enables a ThingDef to host the `CompColorable` component, which allows the weapon's color to be changed or randomized.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CompProperties_Colorable : CompProperties {
    public CompProperties_Colorable() => compClass = typeof(CompColorable);
}