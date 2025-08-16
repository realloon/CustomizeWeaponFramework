using HarmonyLib;
using Verse;

namespace CWF.HarmonyPatches;

[HarmonyPatch(typeof(Thing), nameof(Thing.Graphic), MethodType.Getter)]
public static class Postfix_Thing_Graphic {
    public static void Postfix(Thing __instance, ref Graphic __result) {
        var comp = __instance.TryGetComp<CompDynamicGraphic>();
        if (comp == null) return;

        __result = comp.GetDynamicGraphic();
    }
}