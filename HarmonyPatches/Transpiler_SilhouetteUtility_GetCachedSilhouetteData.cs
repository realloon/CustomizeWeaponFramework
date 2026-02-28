using JetBrains.Annotations;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace CWF.HarmonyPatches;

[HarmonyPatch(typeof(SilhouetteUtility), "GetCachedSilhouetteData")]
public static class Transpiler_SilhouetteUtility_GetCachedSilhouetteData {
    [UsedImplicitly]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        var thingGraphicGetter = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Graphic));
        var replacementMethod = AccessTools.Method(typeof(DynamicGraphicPatchUtility),
            nameof(DynamicGraphicPatchUtility.GetDynamicGraphicOrOriginal));

        foreach (var instruction in instructions) {
            if (instruction.Calls(thingGraphicGetter)) {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacementMethod;
            }

            yield return instruction;
        }
    }
}