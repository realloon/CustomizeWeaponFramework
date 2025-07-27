using UnityEngine;
using HarmonyLib;
using Verse;

namespace CustomizeWeapon.HarmonyPatches;

[HarmonyPatch(typeof(CompColorable), nameof(CompColorable.ForceColor))]
public static class Postfix_CompColorable_ForceColor {
    [HarmonyPostfix]
    public static void Postfix(ThingComp __instance, ref Color? __result) {
        if (__result.HasValue && __instance.parent.TryGetComp<CompDynamicGraphic>() != null) {
            __result = Color.white;
        }
    }
}