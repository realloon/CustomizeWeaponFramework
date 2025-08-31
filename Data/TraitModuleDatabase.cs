using RimWorld;
using UnityEngine;
using Verse;

namespace CWF;

[StaticConstructorOnStartup]
public static class TraitModuleDatabase {
    private static readonly Dictionary<WeaponTraitDef, Part> TraitToPart = new();
    private static readonly Dictionary<WeaponTraitDef, ThingDef> TraitToModule = new();
    private static readonly Dictionary<string, List<ThingDef>> WeaponsByTag = new();

    public static IEnumerable<ThingDef> GetAllModuleDefs() => TraitToModule.Values;

    static TraitModuleDatabase() {
        foreach (var thingDef in DefDatabase<ThingDef>.AllDefs) {
            // fill weapon caches
            if (thingDef.IsWeapon && !thingDef.weaponTags.NullOrEmpty() && thingDef.race == null &&
                !thingDef.IsCorpse) {
                foreach (var tag in thingDef.weaponTags) {
                    if (!WeaponsByTag.ContainsKey(tag)) {
                        WeaponsByTag[tag] = new List<ThingDef>();
                    }

                    WeaponsByTag[tag].Add(thingDef);
                }
            }

            var ext = thingDef.GetModExtension<TraitModuleExtension>();
            if (ext?.weaponTraitDef == null) continue;

            // fill trait caches
            if (TraitToPart.ContainsKey(ext.weaponTraitDef)) {
                Log.Warning(
                    $"[CWF] Cache building warning: WeaponTraitDef '{ext.weaponTraitDef.defName}' is defined by multiple TraitModules. " +
                    $"The one in '{thingDef.defName}' will overwrite previous entries. This may cause unpredictable behavior when uninstalling parts.");
            }

            TraitToPart[ext.weaponTraitDef] = ext.part;
            TraitToModule[ext.weaponTraitDef] = thingDef;
        }

        foreach (var moduleDef in TraitToModule.Values) {
            // inject description
            var description = moduleDef.GetModExtension<TraitModuleExtension>()?.weaponTraitDef?.description;
            if (description != null) {
                moduleDef.description = description;
            }

            // inject hyperlinks
            var weaponDefs = GetCompatibleWeaponDefsFor(moduleDef).ToList();
            if (weaponDefs.NullOrEmpty()) continue;
            moduleDef.descriptionHyperlinks ??= new List<DefHyperlink>();
            foreach (var weaponDef in weaponDefs) {
                if (moduleDef.descriptionHyperlinks.Any(h => h.def == weaponDef)) continue;
                moduleDef.descriptionHyperlinks.Add(new DefHyperlink(weaponDef));
            }
        }

        #if DEBUG
        Log.Message($"[CWF] Built Trait caches with {TraitToPart.Count} entries and injected hyperlinks.");
        #endif
    }

    /// <summary>
    /// Locates the Part that owns the specified WeaponTraitDef.
    /// </summary>
    public static bool TryGetPartForTrait(WeaponTraitDef traitDef, out Part part) {
        return TraitToPart.TryGetValue(traitDef, out part);
    }

    /// <summary>
    /// Looks up the ThingDef of the modular item that provides the specified WeaponTraitDef.
    /// </summary>
    public static ThingDef GetModuleDefFor(WeaponTraitDef traitDef) {
        TraitToModule.TryGetValue(traitDef, out var moduleDef);
        return moduleDef;
    }

    public static List<string> GetTraitEffectLines(WeaponTraitDef traitDef) {
        if (traitDef == null) return new List<string>();

        var effectLines = new List<string>();

        // offset
        if (!traitDef.statOffsets.NullOrEmpty()) {
            effectLines.AddRange(traitDef.statOffsets
                .Where(m => m.stat != StatDefOf.MarketValue && m.stat != StatDefOf.Mass)
                .Select(m =>
                    $" - {m.stat.LabelCap}: {m.stat.Worker.ValueToString(m.value, false, ToStringNumberSense.Offset)}"));
        }

        // factor
        if (!traitDef.statFactors.NullOrEmpty()) {
            effectLines.AddRange(traitDef.statFactors
                .Select(m =>
                    $" - {m.stat.LabelCap}: {m.stat.Worker.ValueToString(m.value, false, ToStringNumberSense.Factor)}"));
        }

        if (!Mathf.Approximately(traitDef.burstShotCountMultiplier, 1f)) {
            effectLines.Add(
                $" - {"CWF_UI_BurstShotCountMultiplier".Translate()}: {traitDef.burstShotCountMultiplier.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor)}");
        }

        if (!Mathf.Approximately(traitDef.burstShotSpeedMultiplier, 1f)) {
            effectLines.Add(
                $" - {"CWF_UI_BurstShotSpeedMultiplier".Translate()}: {traitDef.burstShotSpeedMultiplier.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor)}");
        }

        if (!Mathf.Approximately(traitDef.additionalStoppingPower, 0.0f)) {
            effectLines.Add(
                $" - {"CWF_UI_AdditionalStoppingPower".Translate()}: {traitDef.additionalStoppingPower.ToStringByStyle(ToStringStyle.FloatOne, ToStringNumberSense.Offset)}");
        }

        return effectLines;
    }

    public static bool IsModuleCompatibleWithWeapon(ThingDef moduleDef, ThingDef weaponDef) {
        var ext = moduleDef.GetModExtension<TraitModuleExtension>();
        if (ext == null) return false;

        // exclude first 
        if (ext.excludeWeaponDefs != null && ext.excludeWeaponDefs.Contains(weaponDef)) {
            return false;
        }

        if (!ext.excludeWeaponTags.NullOrEmpty() && !weaponDef.weaponTags.NullOrEmpty()) {
            if (Enumerable.Any(ext.excludeWeaponTags, tag => weaponDef.weaponTags.Contains(tag))) {
                return false;
            }
        }

        var hasRequiredDefs = !ext.requiredWeaponDefs.NullOrEmpty();
        var hasRequiredTags = !ext.requiredWeaponTags.NullOrEmpty();

        // defs
        switch (hasRequiredDefs) {
            case false when !hasRequiredTags:
            case true when ext.requiredWeaponDefs.Contains(weaponDef):
                return true;
        }

        // tags
        if (!hasRequiredTags || weaponDef.weaponTags.NullOrEmpty()) return false;

        return Enumerable.Any(ext.requiredWeaponTags, tag => weaponDef.weaponTags.Contains(tag));
    }

    // Private helper
    private static IEnumerable<ThingDef> GetCompatibleWeaponDefsFor(ThingDef moduleDef) {
        var ext = moduleDef.GetModExtension<TraitModuleExtension>();
        if (ext == null) yield break;

        var results = new HashSet<ThingDef>();

        if (!ext.requiredWeaponDefs.NullOrEmpty()) {
            results.AddRange(ext.requiredWeaponDefs);
        }

        if (!ext.requiredWeaponTags.NullOrEmpty()) {
            foreach (var tag in ext.requiredWeaponTags) {
                if (WeaponsByTag.TryGetValue(tag, out var weapons)) {
                    results.AddRange(weapons);
                }
            }
        }

        // if (ext.requiredWeaponDefs.NullOrEmpty() && ext.requiredWeaponTags.NullOrEmpty()) { }

        if (!ext.excludeWeaponDefs.NullOrEmpty()) {
            results.ExceptWith(ext.excludeWeaponDefs);
        }

        if (!ext.excludeWeaponTags.NullOrEmpty()) {
            foreach (var tag in ext.excludeWeaponTags) {
                if (WeaponsByTag.TryGetValue(tag, out var weaponsToExclude)) {
                    results.ExceptWith(weaponsToExclude);
                }
            }
        }

        foreach (var weaponDef in results) {
            yield return weaponDef;
        }
    }
}