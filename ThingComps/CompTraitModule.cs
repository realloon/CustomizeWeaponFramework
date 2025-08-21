using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

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
}