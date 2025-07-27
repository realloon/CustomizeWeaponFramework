using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace CustomizeWeapon.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.GetGizmos))]
public static class Postfix_Pawn_EquipmentTracker_GetGizmos {
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_EquipmentTracker __instance) {
        foreach (var gizmo in __result) {
            yield return gizmo;
        }

        if (__instance.Primary?.TryGetComp<CompDynamicTraits>() is not { } comp) yield break;

        foreach (var extraGizmo in comp.GetWornGizmosExtra(__instance.pawn)) {
            yield return extraGizmo;
        }
    }
}