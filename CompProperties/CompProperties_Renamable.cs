using Verse;

namespace CWF;

/// <summary>
/// Properties class that enables a ThingDef to host the `CompRenamable` component, allowing the weapon to be given a custom nickname by the player.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CompProperties_Renamable : CompProperties {
    public CompProperties_Renamable() => compClass = typeof(CompRenamable);
}