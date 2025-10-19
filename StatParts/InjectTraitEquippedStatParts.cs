using RimWorld;
using Verse;
using CWF.Extensions;

namespace CWF;

[StaticConstructorOnStartup]
public static class InjectTraitEquippedStatParts {
    static InjectTraitEquippedStatParts() {
        var targets = DefDatabase<StatDef>.AllDefsListForReading
            .Where(stat => stat.showOnPawns && stat.showOnHumanlikes)
            .ToList();

        if (targets.IsNullOrEmpty()) {
            Log.Warning("[CWF] No suitable StatDefs found to inject TraitEquippedOffsets.");
            return;
        }

        foreach (var stat in targets) {
            // try {
            stat.parts ??= [];
            if (stat.parts.Any(p => p is TraitEquippedOffsets)) continue;

            stat.parts.Add(new TraitEquippedOffsets { parentStat = stat });
            // } catch (Exception e) {
            // Log.Error($"[CWF] Failed to inject TraitEquippedOffsets into '{stat?.defName}': {e}");
            // }
        }

        #if DEBUG
        Log.Message("[CWF] Injection of TraitEquippedOffsets complete.");
        #endif
    }
}