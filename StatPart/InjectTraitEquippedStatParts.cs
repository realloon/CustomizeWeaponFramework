using RimWorld;
using Verse;
using HarmonyLib;

namespace CWF;

[StaticConstructorOnStartup]
public static class InjectTraitEquippedStatParts
{
    static InjectTraitEquippedStatParts()
    {
        var targets = new HashSet<StatDef>(DefDatabase<StatDef>.AllDefsListForReading.Where(a => a.showOnPawns && a.showOnHumanlikes && a.showOnNonWorkTables && a.showOnNonPowerPlants));

        if (targets == null || targets.Count == 0)
        {
            if (Prefs.DevMode) Log.Message("[CWF] No StatPartsTargetDef: nothing to inject.");
            return;
        }
        Log.Message($"targets: {targets}");
        Log.Message($"{string.Join("\n", targets.Select(t => t.LabelCap))}");

        foreach (var stat in targets)
        {
            try
            {
                stat.parts ??= [];
                StatPart? part;
                if (stat.ToStringStyleUnfinalized == ToStringStyle.PercentOne || stat.ToStringStyleUnfinalized == ToStringStyle.PercentTwo || stat.ToStringStyleUnfinalized == ToStringStyle.PercentZero)
                {
                    stat.parts.RemoveAll(s => s is TraitEquippedFactors);
                    part = (StatPart)Activator.CreateInstance(typeof(TraitEquippedFactors));
                }
                else
                {
                    stat.parts.RemoveAll(s => s is TraitEquippedOffsets);
                    part = (StatPart)Activator.CreateInstance(typeof(TraitEquippedOffsets));
                }
                part.parentStat = stat;
                stat.parts.Add(part);
            }
            catch (Exception e)
            {
                if (Prefs.DevMode) Log.Warning($"[CWF] Failed to inject StatParts in '{stat?.defName}': {e}");
            }
        }
        if (Prefs.DevMode) Log.Message($"[CWF] Injection complete.");
    }
}
