using HarmonyLib;
using Verse;

namespace CWF.HarmonyPatches;

[HarmonyPatch(typeof(ThingComp), nameof(ThingComp.Notify_Equipped))]
public static class Postfix_ThingComp_Notify_Equipped {
    // ReSharper disable once InconsistentNaming
    public static void Postfix(ThingComp __instance, Pawn pawn) {
        var compAbilityProvider = __instance.parent.GetComp<CompAbilityProvider>();
        compAbilityProvider?.OnEquipped(pawn);
    }
}