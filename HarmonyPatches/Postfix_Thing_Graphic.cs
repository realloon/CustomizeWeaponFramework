using HarmonyLib;
using Verse;

// ReSharper disable InconsistentNaming

namespace CWF.HarmonyPatches;

[HarmonyPatch(typeof(Thing), nameof(Thing.Graphic), MethodType.Getter)]
public static class Postfix_Thing_Graphic {
    public static void Postfix(Thing __instance, ref Graphic __result) {
        if (!__instance.TryGetComp<CompDynamicGraphic>(out var comp)) return;

        __result = comp.GetDynamicGraphic();
    }
}