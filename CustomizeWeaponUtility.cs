using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CustomizeWeapon;

[StaticConstructorOnStartup]
public static class CustomizeWeaponUtility {
    private static readonly Lazy<Caches> AllCaches = new(BuildCaches);

    private struct Caches {
        public Dictionary<WeaponTraitDef, Part> TraitToPart;
        public Dictionary<WeaponTraitDef, ThingDef> TraitToModule;
    }

    // This method is invoked only once, the first time any cache is accessed.
    private static Caches BuildCaches() {
        var traitToPart = new Dictionary<WeaponTraitDef, Part>();
        var traitToModule = new Dictionary<WeaponTraitDef, ThingDef>();

        foreach (var thingDef in DefDatabase<ThingDef>.AllDefs) {
            var ext = thingDef.GetModExtension<TraitModuleExtension>();
            if (ext?.weaponTraitDef == null) continue;

            var trait = ext.weaponTraitDef;
            var part = ext.part;

            // Check for duplicate definitions
            if (traitToPart.ContainsKey(trait)) {
                Log.Warning(
                    $"[CWF] Cache building warning: WeaponTraitDef '{trait.defName}' is defined by multiple TraitModules. " +
                    $"The one in '{thingDef.defName}' will overwrite previous entries. This may cause unpredictable behavior when uninstalling parts.");
            }

            // fill caches
            traitToPart[trait] = part;
            traitToModule[trait] = thingDef;
        }

        Log.Message($"[&CWF Dev] Built Trait caches with {traitToPart.Count} entries.");

        return new Caches {
            TraitToPart = traitToPart,
            TraitToModule = traitToModule
        };
    }

    // === Public helper ===
    /// <summary>
    /// Locates the Part that owns the specified WeaponTraitDef.
    /// </summary>
    /// <param name="traitDef">The trait to locate.</param>
    /// <param name="part">When found, receives the Part that owns the trait.</param>
    /// <returns>true if the Part is located; otherwise, false.</returns>
    public static bool TryGetPartForTrait(WeaponTraitDef traitDef, out Part part) {
        return AllCaches.Value.TraitToPart.TryGetValue(traitDef, out part);
    }

    /// <summary>
    /// Looks up the ThingDef of the modular item that provides the specified WeaponTraitDef.
    /// </summary>
    /// <param name="traitDef">The trait to search for.</param>
    /// <returns>The corresponding modular ThingDef, or null if none is found.</returns>
    public static ThingDef GetModuleDefFor(WeaponTraitDef traitDef) {
        AllCaches.Value.TraitToModule.TryGetValue(traitDef, out var moduleDef);
        return moduleDef;
    }

    /// <summary>
    /// Checks whether a module definition is compatible with the specified weapon instance.
    /// </summary>
    /// <param name="moduleDef">ThingDef of the module to check.</param>
    /// <param name="weapon">Thing instance of the target weapon.</param>
    /// <returns>True if compatible; otherwise, false.</returns>
    public static bool IsModuleCompatibleWithWeapon(ThingDef moduleDef, Thing weapon) {
        var ext = moduleDef.GetModExtension<TraitModuleExtension>();
        if (ext == null) return false;

        var weaponDef = weapon.def;

        // exclude first 
        if (ext.excludeWeaponDefs != null && ext.excludeWeaponDefs.Contains(weaponDef)) {
            return false;
        }

        if (!ext.excludeWeaponTags.NullOrEmpty() && !weaponDef.weaponTags.NullOrEmpty()) {
            if (ext.excludeWeaponTags.Any(tag => weaponDef.weaponTags.Contains(tag))) {
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

        return ext.requiredWeaponTags.Any(tag => weaponDef.weaponTags.Contains(tag));
    }

    /// <summary>
    /// Retrieves all compatible module definitions for a given part slot on the specified weapon.
    /// </summary>
    /// <param name="part">The part slot to query, e.g., Part.Sight.</param>
    /// <param name="weapon">The weapon instance being modified.</param>
    /// <returns>An enumerable collection of all compatible module ThingDefs.</returns>
    public static IEnumerable<ThingDef> GetCompatibleModulesForPart(Part part, Thing weapon) {
        return AllCaches.Value.TraitToModule.Values
            .Where(moduleDef => moduleDef.GetModExtension<TraitModuleExtension>().part == part)
            .Where(moduleDef => IsModuleCompatibleWithWeapon(moduleDef, weapon));
    }

    /// <summary>
    /// Finds the most suitable graphic data for a given module trait when applied to a specific weapon.
    /// It resolves the graphic based on the matching rules and priority defined in the module's TraitModuleExtension.
    /// </summary>
    /// <param name="traitDef">The weapon trait of the installed module.</param>
    /// <param name="weapon">The weapon instance on which the module is installed.</param>
    /// <returns>The best-matching ModuleGraphicData, or null if no suitable graphic rule is found.</returns>
    public static ModuleGraphicData GetGraphicDataFor(WeaponTraitDef traitDef, Thing weapon) {
        if (traitDef == null || weapon == null) return null;

        var moduleDef = GetModuleDefFor(traitDef);
        if (moduleDef == null) return null;

        var ext = moduleDef.GetModExtension<TraitModuleExtension>();
        if (ext?.graphicCases.NullOrEmpty() ?? true) return null;

        var matchingCases = ext.graphicCases
            .Where(c => c.matcher != null && c.graphicData != null && c.matcher.IsMatch(weapon.def))
            .ToList();

        if (!matchingCases.Any()) {
            Log.Warning(
                $"[CWF] No suitable 'graphicCases' found for module '{moduleDef.defName}' on weapon '{weapon.def.defName}'. Check the mod extension XML.");
            return null;
        }

        var bestCase = matchingCases.MaxBy(c => c.priority);
        return bestCase.graphicData;
    }
}