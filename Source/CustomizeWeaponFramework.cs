using JetBrains.Annotations;
using HarmonyLib;
using Verse;

namespace CWF;

[UsedImplicitly]
[StaticConstructorOnStartup]
public class CustomizeWeaponFramework {
    static CustomizeWeaponFramework() {
        var harmony = new Harmony("Vortex.CustomizeWeaponFramework");
        harmony.PatchAll();

        AdapterDef.Inject();
        ModuleDatabase.BuildCacheAndInject();
        TraitEquippedOffsets.Inject();
    }
}