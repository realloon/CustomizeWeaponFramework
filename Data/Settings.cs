using Verse;

namespace CWF;

public class Settings : ModSettings {
    private const bool DefaultRandomModulesEnabled = true;
    private const int DefaultMinRandomModules = 0;
    private const int DefaultMaxRandomModules = 3;

    public bool RandomModulesEnabled = DefaultRandomModulesEnabled;
    public int MinRandomModules = DefaultMinRandomModules;
    public int MaxRandomModules = DefaultMaxRandomModules;

    public void Reset() {
        RandomModulesEnabled = DefaultRandomModulesEnabled;
        MinRandomModules = DefaultMinRandomModules;
        MaxRandomModules = DefaultMaxRandomModules;
    }

    public override void ExposeData() {
        base.ExposeData();
        Scribe_Values.Look(ref RandomModulesEnabled, "randomModulesEnabled", DefaultRandomModulesEnabled);
        Scribe_Values.Look(ref MinRandomModules, "minRandomModules", DefaultMinRandomModules);
        Scribe_Values.Look(ref MaxRandomModules, "maxRandomModules", DefaultMaxRandomModules);
    }
}