using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace CWF;

[UsedImplicitly]
public class ConfigWindow : Mod {
    private readonly Settings _settings;

    public ConfigWindow(ModContentPack content) : base(content) {
        _settings = GetSettings<Settings>();
    }

    public override string SettingsCategory() => "Customize Weapon";

    public override void DoSettingsWindowContents(Rect inRect) {
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        listing.CheckboxLabeled("CWF_UI_RandomModuleGeneration".Translate(), ref _settings.RandomModulesEnabled,
            "CWF_UI_RandomModuleGenerationDesc".Translate());

        if (_settings.RandomModulesEnabled) {
            var range = new IntRange(_settings.MinRandomModules, _settings.MaxRandomModules);
            listing.IntRange(ref range, 0, 10);

            _settings.MinRandomModules = range.min;
            _settings.MaxRandomModules = range.max;
        }

        listing.Gap(24f);

        if (listing.ButtonText("Reset".Translate(), widthPct: 0.5f)) {
            _settings.Reset();
        }

        listing.End();
        base.DoSettingsWindowContents(inRect);
    }
}