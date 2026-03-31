using JetBrains.Annotations;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;

// ReSharper disable InconsistentNaming

namespace CWF.HarmonyPatches;

[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
public static class Postfix_ThingDef_SpecialDisplayStats_Modules {
    [UsedImplicitly]
    public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> __result, ThingDef __instance) {
        foreach (var entry in __result) {
            yield return entry;
        }

        var ext = __instance.GetModExtension<TraitModuleExtension>();
        if (ext?.weaponTraitDef == null) yield break;

        var traitDef = ext.weaponTraitDef;
        var part = ext.part;

        var sb = new StringBuilder();
        var effect = traitDef.GetTraitEffect();

        if (effect.Any()) {
            sb.AppendLine("CWF_ModuleEffectsDesc".Translate(traitDef.Named("MODULE")) + ":");
            sb.AppendLine();
            sb.AppendLine(effect);
        }

        yield return new StatDrawEntry(
            CWF_DefOf.CWF_WeaponModule,
            "CWF_ModuleEffects".Translate(),
            traitDef.LabelCap,
            sb.ToString().TrimEndNewlines(),
            1000
        );

        yield return new StatDrawEntry(
            CWF_DefOf.CWF_WeaponModule,
            "CWF_PartOf".Translate(),
            part.LabelCap,
            "CWF_PartOf".Translate() + ": " + part.LabelCap,
            999
        );
    }
}
