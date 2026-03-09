using UnityEngine;
using Verse;

namespace CWF;

public class Settings : ModSettings {
    private const bool DefaultRandomModulesEnabled = true;
    private const int DefaultMinRandomModules = 0;
    private const int DefaultMaxRandomModules = 3;
    internal const float DefaultModuleSellPriceFactor = 0.2f;

    public bool RandomModulesEnabled = DefaultRandomModulesEnabled;
    public int MinRandomModules = DefaultMinRandomModules;
    public int MaxRandomModules = DefaultMaxRandomModules;
    public float ModuleSellPriceFactor = DefaultModuleSellPriceFactor;

    internal const float MinModuleSellPriceFactor = 0.01f;
    internal const float MaxModuleSellPriceFactor = 1f;

    public void Reset() {
        RandomModulesEnabled = DefaultRandomModulesEnabled;
        MinRandomModules = DefaultMinRandomModules;
        MaxRandomModules = DefaultMaxRandomModules;
        ModuleSellPriceFactor = DefaultModuleSellPriceFactor;
    }

    public override void ExposeData() {
        base.ExposeData();
        Scribe_Values.Look(ref RandomModulesEnabled, "randomModulesEnabled", DefaultRandomModulesEnabled);
        Scribe_Values.Look(ref MinRandomModules, "minRandomModules", DefaultMinRandomModules);
        Scribe_Values.Look(ref MaxRandomModules, "maxRandomModules", DefaultMaxRandomModules);
        Scribe_Values.Look(ref ModuleSellPriceFactor, "moduleSellPriceFactor", DefaultModuleSellPriceFactor);

        // Legacy key compatibility for old saves/configs
        var legacyModuleSellPriceFactor = ModuleSellPriceFactor;
        Scribe_Values.Look(ref legacyModuleSellPriceFactor, "customDifficultyModuleSellPriceFactor",
            ModuleSellPriceFactor);
        ModuleSellPriceFactor = legacyModuleSellPriceFactor;

        ModuleSellPriceFactor = Mathf.Clamp(ModuleSellPriceFactor,
            MinModuleSellPriceFactor, MaxModuleSellPriceFactor);
    }
}