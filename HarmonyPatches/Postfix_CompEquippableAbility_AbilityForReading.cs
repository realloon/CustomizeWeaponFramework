using System.Reflection;
using HarmonyLib;
using RimWorld;

namespace CWF.HarmonyPatches;

[HarmonyPatch(typeof(CompEquippableAbility), "get_AbilityForReading")]
public static class Postfix_CompEquippableAbility_AbilityForReading {
    private static readonly FieldInfo abilityField = AccessTools.Field(typeof(CompEquippableAbility), "ability");

    [HarmonyPostfix]
    public static void Postfix(CompEquippableAbility __instance, ref Ability __result) {
        var props = __instance.props as CompProperties_EquippableAbility;
        var expectedAbilityDef = props?.abilityDef;

        if ((__result == null || __result.def == expectedAbilityDef) &&
            (__result == null || expectedAbilityDef != null)) return;

        if (abilityField != null) {
            abilityField.SetValue(__instance, null);
        }

        __result = __instance.AbilityForReading;
    }
}