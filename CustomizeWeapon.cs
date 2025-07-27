using HarmonyLib;
using Verse;

namespace CustomizeWeapon;

[StaticConstructorOnStartup]
public class CustomizeWeapon {
    static CustomizeWeapon() {
        var harmony = new Harmony("Vortex.CustomizeWeapon");
        harmony.PatchAll();
    }
}