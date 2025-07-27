using System;
using System.Collections.Generic;
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
}