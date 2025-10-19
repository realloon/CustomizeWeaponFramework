using CWF.Extensions;
using RimWorld;
using System.Text;
using Verse;

namespace CWF;

public class TraitEquippedOffsets : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        if (!req.HasThing || req.Thing is not Pawn pawn) return;

        var weapon = pawn?.equipment?.Primary;
        if (weapon == null || weapon.DestroyedOrNull() || weapon.def == null) return;

        var traits = weapon?.GetComp<CompDynamicTraits>()?.Traits;
        if (traits == null || traits.Count == 0) return;

        var stat = parentStat;
        float add = 0f;
        foreach (var trait in traits)
        {
            var statOffSets = trait?.equippedStatOffsets?.Where(t => t?.stat == stat).ToList();
            if (statOffSets.IsNullOrEmpty()) continue;

            add += statOffSets.Sum(s => s.value);
        }
        if (add != 0f) val += add;
    }

    public override string ExplanationPart(StatRequest req)
    {
        if (!req.HasThing || req.Thing is not Pawn pawn) return "";

        var weapon = pawn.equipment?.Primary;
        if (weapon == null) return "";

        var traits = weapon?.GetComp<CompDynamicTraits>()?.Traits;
        if (traits == null || traits.Count == 0) return "";

        var stat = parentStat;
        var sb = new StringBuilder();
        sb.AppendLine($"Weapon {weapon.Label} traits: ");

        float total = 0f;
        foreach (var t in traits)
        {
            var offSets = t?.equippedStatOffsets?.Where(o => o.stat == stat)?.ToList();
            if (offSets.IsNullOrEmpty()) continue;

            float local = offSets.Sum(s => s.value);
            if (local != 0f)
            {
                total += local;
                sb.AppendLine($"    Trait {t.label}: +{stat.ValueToString(local)}");
            }
        }
        if (total == 0f) return "";

        return sb.ToString();
    }

    public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest req)
    {
        if (!req.HasThing || req.Thing is not Pawn pawn) yield break;

        var weapon = pawn.equipment?.Primary;
        if (weapon != null)
        {
            yield return new Dialog_InfoCard.Hyperlink(weapon);
        }
        var traits = weapon?.GetComp<CompDynamicTraits>()?.Traits;
        if (traits == null) yield break;

        var seen = new HashSet<Def>();
        foreach (var t in traits)
        {
            var factors = t?.equippedStatOffsets?.Where(o => o.stat == parentStat)?.ToList();
            if (factors == null) continue;

            if (t == null || !seen.Add(t) || !TraitModuleDatabase.TryGetModuleDef(t, out ThingDef? traitDef)) continue;
            yield return new Dialog_InfoCard.Hyperlink(traitDef);
        }
    }
}
