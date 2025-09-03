using UnityEngine;
using RimWorld;
using Verse;

namespace CWF;

public class Recipe_UpgradeWeapon : RecipeWorker {
    // Cache: Key is the base weaponDef, Value is the custom weaponDef.
    private static Dictionary<ThingDef, ThingDef> _upgradePaths = new();

    // Static constructor, runs once when the game loads.
    static Recipe_UpgradeWeapon() {
        BuildUpgradePathsCache();
    }

    /// <summary>
    /// Scans all ThingDefs for weapons with UpgradeExtension and builds the cache.
    /// </summary>
    private static void BuildUpgradePathsCache() {
        _upgradePaths = new Dictionary<ThingDef, ThingDef>();
        var allThingDefs = DefDatabase<ThingDef>.AllDefs;

        foreach (var thingDef in allThingDefs) {
            var ext = thingDef.GetModExtension<UpgradeExtension>();
            if (ext?.baseWeaponDef == null) continue;

            if (_upgradePaths.TryGetValue(ext.baseWeaponDef, out var path)) {
                Log.Warning($"[CWF] Duplicate upgrade path detected for '{ext.baseWeaponDef.defName}'. " +
                            $"It is already mapped to '{path.defName}'. " +
                            $"The new mapping to '{thingDef.defName}' will be ignored.");
            } else {
                _upgradePaths[ext.baseWeaponDef] = thingDef;
            }
        }

        #if DEBUG
        Log.Message($"[CWF Dev] Built weapon upgrade path cache with {_upgradePaths.Count} entries.");
        #endif
    }

    /// <summary>
    /// Called by the bill menu to check if a given 'thing' is a valid ingredient.
    /// </summary>
    public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord? _ = null) {
        return _upgradePaths.ContainsKey(thing.def)
            ? AcceptanceReport.WasAccepted
            : new AcceptanceReport("CWF_Message_WeaponCannotBeUpgraded".Translate(thing.LabelShortCap));
    }

    /// <summary>
    /// Called when a crafting iteration is completed. This is where we create the new item.
    /// </summary>
    public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients) {
        base.Notify_IterationCompleted(billDoer, ingredients);

        // Find the base weapon from the ingredients list.
        var baseWeapon = Enumerable.FirstOrDefault(ingredients, ing => _upgradePaths.ContainsKey(ing.def));
        if (baseWeapon == null) {
            Log.Error("[CWF] Recipe_UpgradeWeapon's Notify_IterationCompleted was called, " +
                      "but no valid base weapon was found in ingredients.");
            return;
        }

        // Get the ThingDef for the custom weapon to be created.
        var customWeaponDef = _upgradePaths[baseWeapon.def];

        // Read the quality and durability from the original weapon.
        var hasQuality = baseWeapon.TryGetComp<CompQuality>(out var qualityComp);
        var quality = hasQuality ? qualityComp.Quality : QualityCategory.Good;

        var maxHitPoints = baseWeapon.MaxHitPoints;
        // This is the direct, simplified way to get the current durability.
        var currentHitPoints = baseWeapon.HitPoints;

        // Create the new custom weapon instance.
        var customWeapon = ThingMaker.MakeThing(customWeaponDef);

        // Apply the saved quality and durability to the new weapon.
        customWeapon.TryGetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Colony);

        // Calculate the health percentage.
        var hitPoints = (float)currentHitPoints / maxHitPoints;
        // Calculate the new weapon's target hit points.
        var newHitPoints = customWeapon.MaxHitPoints * hitPoints;

        // Set the final hit points, using RoundRandom and ensuring it's at least 1.
        customWeapon.HitPoints = Mathf.Max(1, GenMath.RoundRandom(newHitPoints));

        // Spawn the newly created weapon near the crafter.
        GenPlace.TryPlaceThing(customWeapon, billDoer.Position, billDoer.Map, ThingPlaceMode.Near);
    }
}