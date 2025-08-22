using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CWF;

public class CompTraitModule : ThingComp {
    // private CompProperties_TraitModule Props => (CompProperties_TraitModule)props;

    // private WeaponTraitDef _cachedTraitDef;
    // private bool _isTraitDefCached;

    // private WeaponTraitDef TraitDef {
    //     get {
    //         if (_isTraitDefCached) return _cachedTraitDef;
    //
    //         _cachedTraitDef = parent.def.GetModExtension<TraitModuleExtension>()?.weaponTraitDef;
    //         _isTraitDefCached = true;
    //         return _cachedTraitDef;
    //     }
    // }

    public static List<string> GetTraitEffectLines(WeaponTraitDef traitDef) {
        if (traitDef == null) return new List<string>();

        var effectLines = new List<string>();

        // offset
        if (!traitDef.statOffsets.NullOrEmpty()) {
            effectLines.AddRange(traitDef.statOffsets.Select(offset =>
                $" - {offset.stat.LabelCap}: {offset.stat.Worker.ValueToString(offset.value, false, ToStringNumberSense.Offset)}"));
        }

        // factor
        if (!traitDef.statFactors.NullOrEmpty()) {
            effectLines.AddRange(traitDef.statFactors.Select(factor =>
                $" - {factor.stat.LabelCap}: {factor.stat.Worker.ValueToString(factor.value, false, ToStringNumberSense.Factor)}"));
        }

        if (!Mathf.Approximately(traitDef.burstShotCountMultiplier, 1f)) {
            effectLines.Add(
                $" - {"CWF_BurstShotCountFactor".Translate()}: {traitDef.burstShotCountMultiplier.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor)}");
        }

        if (!Mathf.Approximately(traitDef.burstShotSpeedMultiplier, 1f)) {
            effectLines.Add(
                $" - {"CWF_TicksBetweenBurstShotsFactor".Translate()}: {traitDef.burstShotSpeedMultiplier.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor)}");
        }

        if (!Mathf.Approximately(traitDef.additionalStoppingPower, 0.0f)) {
            effectLines.Add(
                $" - {"StatsReport_AdditionalStoppingPower".Translate()} {traitDef.additionalStoppingPower.ToStringByStyle(ToStringStyle.FloatOne, ToStringNumberSense.Offset)}");
        }

        return effectLines;
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn) {
        foreach (var option in base.CompFloatMenuOptions(selPawn)) {
            yield return option;
        }

        if (parent.IsForbidden(selPawn)) yield break;
        if (!selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) yield break;

        if (!selPawn.CanReserveAndReach(parent, PathEndMode.ClosestTouch, Danger.Deadly)) {
            yield return new FloatMenuOption("CannotReach".Translate(parent.LabelCap), null);
            yield break;
        }

        yield return new FloatMenuOption(
            "CWF_UI_PickUp".Translate(parent.Named("MODULE")),
            () => {
                var job = JobMaker.MakeJob(JobDefOf.TakeInventory, parent);
                job.count = 1;
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        );
    }
}