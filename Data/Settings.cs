using UnityEngine;
using Verse;

namespace CWF;

public class Settings : ModSettings {
    private const bool DefaultRandomModulesEnabled = true;
    private const int DefaultMinRandomModules = 0;
    private const int DefaultMaxRandomModules = 3;

    public bool randomModulesEnabled = DefaultRandomModulesEnabled;
    public int minRandomModules = DefaultMinRandomModules;
    public int maxRandomModules = DefaultMaxRandomModules;

    public void Reset() {
        randomModulesEnabled = DefaultRandomModulesEnabled;
        minRandomModules = DefaultMinRandomModules;
        maxRandomModules = DefaultMaxRandomModules;
    }

    public override void ExposeData() {
        base.ExposeData();
        Scribe_Values.Look(ref randomModulesEnabled, "randomModulesEnabled", DefaultRandomModulesEnabled);
        Scribe_Values.Look(ref minRandomModules, "minRandomModules", DefaultMinRandomModules);
        Scribe_Values.Look(ref maxRandomModules, "maxRandomModules", DefaultMaxRandomModules);
    }
}