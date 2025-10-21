using HarmonyLib;
using Verse;

namespace CWF.HarmonyPatches;

[HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
public static class Prefix_GenRecipe_MakeRecipeProducts {
    [HarmonyPrefix]
    public static void Prefix() {
        CreationContext.IsPlayerCrafting = true;
    }
}