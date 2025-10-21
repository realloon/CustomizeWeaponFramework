using HarmonyLib;
using Verse;

namespace CWF.HarmonyPatches;

[HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
public static class Patch_GenRecipe_MakeRecipeProducts {
    [HarmonyPrefix]
    public static void Prefix() {
        CreationContext.IsPlayerCrafting = true;
    }

    [HarmonyPostfix]
    public static void Postfix() {
        CreationContext.IsPlayerCrafting = false;
    }
}