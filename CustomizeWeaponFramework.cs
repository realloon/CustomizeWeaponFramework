using HarmonyLib;
using Verse;

namespace CWF;

[StaticConstructorOnStartup]
public class CustomizeWeaponFramework {
    static CustomizeWeaponFramework() {
        var harmony = new Harmony("Vortex.CustomizeWeaponFramework");
        harmony.PatchAll();
    }
}