using Verse;

namespace CustomizeWeapon;

public class CompProperties_Renamable : CompProperties {
    public CompProperties_Renamable() => compClass = typeof(CompRenamable);
}