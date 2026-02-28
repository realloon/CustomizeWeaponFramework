using System.Text;
using UnityEngine;
using RimWorld;
using Verse;
using CWF.Extensions;

namespace CWF;

public class TraitEquippedOffsets : StatPart {
    public override void TransformValue(StatRequest req, ref float val) {
        if (!TryGetApplicableTraits(req, out var traits)) return;

        var totalOffset = 0f;

        for (var i = 0; i < traits.Count; i++) {
            var modifiers = traits[i].equippedStatOffsets;
            if (modifiers.NullOrEmpty()) continue;

            for (var j = 0; j < modifiers.Count; j++) {
                var modifier = modifiers[j];
                if (modifier?.stat == parentStat) {
                    totalOffset += modifier.value;
                }
            }
        }

        if (!Mathf.Approximately(totalOffset, 0f)) {
            val += totalOffset;
        }
    }

    public override string ExplanationPart(StatRequest req) {
        if (!TryGetApplicableTraits(req, out var traits)) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.Append("CWF_UI_FromEquipped".Translate());

        for (var i = 0; i < traits.Count; i++) {
            var trait = traits[i];
            var modifiers = trait.equippedStatOffsets;
            if (modifiers.NullOrEmpty()) continue;

            var localOffset = 0f;
            for (var j = 0; j < modifiers.Count; j++) {
                var modifier = modifiers[j];
                if (modifier?.stat == parentStat) {
                    localOffset += modifier.value;
                }
            }

            if (Mathf.Approximately(localOffset, 0f)) continue;

            var valueStr = parentStat.ValueToString(localOffset, numberSense: ToStringNumberSense.Offset);
            sb.AppendInNewLine($"    {trait.LabelCap}: {valueStr}"); // consistent with interface format
        }
        
        sb.AppendLine();

        return sb.ToString();
    }

    public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest req) {
        if (!TryGetApplicableTraits(req, out var traits)) yield break;

        var seenModules = new HashSet<ThingDef>();
        for (var i = 0; i < traits.Count; i++) {
            var trait = traits[i];
            if (trait.TryGetModuleDef(out var moduleDef) && seenModules.Add(moduleDef)) {
                yield return new Dialog_InfoCard.Hyperlink(moduleDef);
            }
        }
    }

    internal static void Inject() {
        var targetStats = new HashSet<StatDef>();
        var allTraits = DefDatabase<WeaponTraitDef>.AllDefsListForReading;

        for (var i = 0; i < allTraits.Count; i++) {
            var modifiers = allTraits[i].equippedStatOffsets;
            if (modifiers.NullOrEmpty()) continue;

            for (var j = 0; j < modifiers.Count; j++) {
                var modifier = modifiers[j];
                if (modifier?.stat == null) continue;
                if (!modifier.stat.showOnPawns || !modifier.stat.showOnHumanlikes) continue;

                targetStats.Add(modifier.stat);
            }
        }

        if (targetStats.IsNullOrEmpty()) {
            Log.Warning("[CWF] No suitable StatDefs found to inject TraitEquippedOffsets.");
            return;
        }

        foreach (var stat in targetStats) {
            stat.parts ??= [];
            if (stat.parts.Any(p => p is TraitEquippedOffsets)) continue;

            stat.parts.Add(new TraitEquippedOffsets { parentStat = stat });
        }
    }

    // helper
    private bool TryGetApplicableTraits(StatRequest req, out IReadOnlyList<WeaponTraitDef> applicableTraits) {
        applicableTraits = [];
        if (!req.HasThing || req.Thing is not Pawn pawn) return false;

        var weapon = pawn.equipment?.Primary;
        if (weapon == null) return false;

        var traits = weapon.TryGetComp<CompDynamicTraits>()?.Traits;
        if (traits.IsNullOrEmpty()) return false;

        var list = new List<WeaponTraitDef>();

        foreach (var trait in traits) {
            if (trait == null || trait.equippedStatOffsets.NullOrEmpty()) continue;

            var hasMatch = false;
            for (var i = 0; i < trait.equippedStatOffsets.Count; i++) {
                if (trait.equippedStatOffsets[i]?.stat == parentStat) {
                    hasMatch = true;
                    break;
                }
            }

            if (hasMatch) {
                list.Add(trait);
            }
        }

        if (list.Count == 0) return false;

        applicableTraits = list;
        return true;
    }
}
