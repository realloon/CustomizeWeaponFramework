using System.Text;
using RimWorld;
using Verse;
using CWF.Extensions;

namespace CWF;

public class TraitEquippedOffsets : StatPart {
    public override void TransformValue(StatRequest req, ref float val) {
        var traits = GetApplicableTraits(req);
        if (traits == null) return;

        var totalOffset = traits.Sum(trait => trait.equippedStatOffsets?.Where(m => m?.stat == parentStat)
            .Sum(m => m.value) ?? 0f);

        val += totalOffset;
    }

    public override string ExplanationPart(StatRequest req) {
        var traits = GetApplicableTraits(req);
        if (traits == null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("CWF_UI_FromEquipped".Translate());

        foreach (var trait in traits) {
            var localOffset = trait.equippedStatOffsets?
                .Where(mod => mod?.stat == parentStat)
                .Sum(mod => mod.value) ?? 0f;

            if (localOffset == 0f) continue;

            var valueStr = parentStat.ValueToString(localOffset, numberSense: ToStringNumberSense.Offset);
            sb.AppendLine($"    {trait.LabelCap}: {valueStr}"); // consistent with interface format
        }

        return sb.ToString(); // \n
    }

    public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest req) {
        var traits = GetApplicableTraits(req);

        if (traits == null) yield break;

        var seenModules = new HashSet<ThingDef>();
        foreach (var trait in traits) {
            if (trait.TryGetModuleDef(out var moduleDef) && seenModules.Add(moduleDef)) {
                yield return new Dialog_InfoCard.Hyperlink(moduleDef);
            }
        }
    }

    // helper
    private IReadOnlyCollection<WeaponTraitDef>? GetApplicableTraits(StatRequest req) {
        if (!req.HasThing || req.Thing is not Pawn pawn) return null;

        var weapon = pawn.equipment?.Primary;
        if (weapon == null) return null;

        var traits = weapon.TryGetComp<CompDynamicTraits>()?.Traits;
        if (traits.IsNullOrEmpty()) return null;

        var applicableTraits = traits
            .Where(trait =>
                trait != null &&
                trait.equippedStatOffsets?.Any(m => m?.stat == parentStat) == true)
            .ToList();

        return !applicableTraits.IsNullOrEmpty() ? applicableTraits : null;
    }
}