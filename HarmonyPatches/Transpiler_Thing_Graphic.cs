using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace CWF.HarmonyPatches;

[HarmonyPatch(typeof(Thing), nameof(Thing.Graphic), MethodType.Getter)]
public static class Transpiler_Thing_Graphic {
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
        var getDynamicGraphicMethod = AccessTools.Method(typeof(CompDynamicGraphic),
            nameof(CompDynamicGraphic.GetDynamicGraphic));
        var compsField = AccessTools.Field(typeof(ThingWithComps), "comps");
        var listGetItemMethod = AccessTools.Method(typeof(List<ThingComp>), "get_Item");
        var listGetCountMethod = AccessTools.Method(typeof(List<ThingComp>), "get_Count");

        var thingWithCompsVar = il.DeclareLocal(typeof(ThingWithComps));
        var compListVar = il.DeclareLocal(typeof(List<ThingComp>));
        var loopIndexVar = il.DeclareLocal(typeof(int));
        var currentCompVar = il.DeclareLocal(typeof(ThingComp));

        var originalCodeLabel = il.DefineLabel();
        var loopStartLabel = il.DefineLabel();
        var loopCheckLabel = il.DefineLabel();

        var code = new List<CodeInstruction> {
            new(OpCodes.Ldarg_0),
            new(OpCodes.Isinst, typeof(ThingWithComps)),
            new(OpCodes.Stloc, thingWithCompsVar),
            new(OpCodes.Ldloc, thingWithCompsVar),
            new(OpCodes.Brfalse_S, originalCodeLabel),
            new(OpCodes.Ldloc, thingWithCompsVar),
            new(OpCodes.Ldfld, compsField),
            new(OpCodes.Stloc, compListVar),
            new(OpCodes.Ldloc, compListVar),
            new(OpCodes.Brfalse_S, originalCodeLabel),
            new(OpCodes.Ldc_I4_0),
            new(OpCodes.Stloc, loopIndexVar),
            new(OpCodes.Br_S, loopCheckLabel),
            new CodeInstruction(OpCodes.Nop).WithLabels(loopStartLabel),
            new(OpCodes.Ldloc, compListVar),
            new(OpCodes.Ldloc, loopIndexVar),
            new(OpCodes.Callvirt, listGetItemMethod),
            new(OpCodes.Stloc, currentCompVar),
            new(OpCodes.Ldloc, currentCompVar),
            new(OpCodes.Isinst, typeof(CompDynamicGraphic))
        };

        var foundCompLabel = il.DefineLabel();
        code.Add(new CodeInstruction(OpCodes.Brtrue_S, foundCompLabel));

        var incrementLabel = il.DefineLabel();
        code.Add(new CodeInstruction(OpCodes.Nop).WithLabels(incrementLabel));
        code.Add(new CodeInstruction(OpCodes.Ldloc, loopIndexVar));
        code.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
        code.Add(new CodeInstruction(OpCodes.Add));
        code.Add(new CodeInstruction(OpCodes.Stloc, loopIndexVar));

        code.Add(new CodeInstruction(OpCodes.Nop).WithLabels(loopCheckLabel));
        code.Add(new CodeInstruction(OpCodes.Ldloc, loopIndexVar));
        code.Add(new CodeInstruction(OpCodes.Ldloc, compListVar));
        code.Add(new CodeInstruction(OpCodes.Callvirt, listGetCountMethod));
        code.Add(new CodeInstruction(OpCodes.Blt_S, loopStartLabel));

        code.Add(new CodeInstruction(OpCodes.Br_S, originalCodeLabel));

        code.Add(new CodeInstruction(OpCodes.Nop).WithLabels(foundCompLabel));
        code.Add(new CodeInstruction(OpCodes.Ldloc, currentCompVar));
        code.Add(new CodeInstruction(OpCodes.Isinst, typeof(CompDynamicGraphic)));
        code.Add(new CodeInstruction(OpCodes.Callvirt, getDynamicGraphicMethod));
        code.Add(new CodeInstruction(OpCodes.Ret));

        var originalInstructions = instructions.ToList();
        if (originalInstructions.Any()) {
            originalInstructions.First().labels.Add(originalCodeLabel);
        }

        code.AddRange(originalInstructions);

        return code.AsEnumerable();
    }
}