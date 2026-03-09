using System.Linq;
using RimWorld;
using Verse;

namespace CWF;

public class ModuleSellPriceFactor : StatPart {
    public override void TransformValue(StatRequest req, ref float val) {
        if (!TryGetModuleDef(req, out _)) return;

        val *= GetMultiplier();
    }

    public override string ExplanationPart(StatRequest req) {
        if (!TryGetModuleDef(req, out _)) return string.Empty;

        var multiplier = GetMultiplier();
        return "× " + multiplier.ToString("F2") + " (" + "ItemSellPriceFactor".Translate() + ")";
    }

    internal static void Inject() {
        var stat = StatDefOf.SellPriceFactor;
        stat.parts ??= [];

        if (stat.parts.Any(p => p is ModuleSellPriceFactor)) return;

        stat.parts.Add(new ModuleSellPriceFactor {
            parentStat = stat
        });
    }

    private static bool TryGetModuleDef(StatRequest req, out ThingDef moduleDef) {
        moduleDef = null!;
        if (!req.HasThing) return false;

        var def = req.Thing?.def;
        if (def == null || def.GetModExtension<TraitModuleExtension>() == null) return false;

        moduleDef = def;
        return true;
    }

    private static float GetMultiplier() {
        var settings = LoadedModManager.GetMod<ConfigWindow>()?.GetSettings<Settings>();
        return settings?.ModuleSellPriceFactor ?? Settings.DefaultModuleSellPriceFactor;
    }
}
