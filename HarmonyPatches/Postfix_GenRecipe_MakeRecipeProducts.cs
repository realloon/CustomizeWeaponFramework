using HarmonyLib;
using Verse;

namespace CWF.HarmonyPatches;

[HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
public static class Postfix_GenRecipe_MakeRecipeProducts {
    [HarmonyPostfix]
    public static void Postfix() {
        CreationContext.IsPlayerCrafting = false;
    }
}