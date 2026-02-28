using Verse;

namespace CWF.HarmonyPatches;

internal static class DynamicGraphicPatchUtility {
    internal static Graphic GetDynamicGraphicOrOriginal(Thing thing) {
        return thing.TryGetComp<CompDynamicGraphic>(out var compDynamicGraphic)
            ? compDynamicGraphic.GetDynamicGraphic()
            : thing.Graphic;
    }
}