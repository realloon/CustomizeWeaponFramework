using System.Text;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using Verse;
using CWF.Extensions;

namespace CWF;

public class CompDynamicTraits : ThingComp {
    private CompProperties_DynamicTraits Props => (CompProperties_DynamicTraits)props;

    private Dictionary<Part, WeaponTraitDef> _installedTraits = new();

    private HashSet<Part> _availableParts = new();

    /// <summary>
    /// Gets a copy of the dictionary containing all currently installed Traits,
    /// or sets a new dictionary of Traits, completely overwriting the old one.
    /// </summary>
    public Dictionary<Part, WeaponTraitDef> InstalledTraits {
        get => new(_installedTraits);
        set {
            _installedTraits = new Dictionary<Part, WeaponTraitDef>(value);
            OnTraitsChanged();
        }
    }

    public IReadOnlyCollection<WeaponTraitDef> Traits => _installedTraits.Values;

    public IReadOnlyCollection<Part> AvailableParts => _availableParts;

    public void InstallTrait(Part part, WeaponTraitDef traitDef) {
        if (!_installedTraits.TryAdd(part, traitDef)) {
            Log.Error(
                $"[CWF] Trying to install part {traitDef.defName} into already occupied slot {part} on {parent.LabelCap}.");
            return;
        }

        OnTraitsChanged();
    }

    public void UninstallTrait(Part part) {
        if (!_installedTraits.Remove(part)) return;

        OnTraitsChanged();
    }

    // public void ClearTraits() {
    //     _installedTraits.Clear();
    //
    //     OnTraitsChanged();
    // }

    // === Stat ===
    public override float GetStatOffset(StatDef stat) {
        return Traits
            .Sum(traitDef => traitDef.statOffsets.GetStatOffsetFromList(stat));
    }

    public override float GetStatFactor(StatDef stat) {
        return Traits
            .Aggregate(1f, (current, traitDef)
                => current * traitDef.statFactors.GetStatFactorFromList(stat));
    }

    // === Display ===
    public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "") {
        StringBuilder? stringBuilder = null;

        foreach (var weaponTraitDef in Traits) {
            // offset
            var statOffsetFromList = weaponTraitDef.statOffsets.GetStatOffsetFromList(stat);
            if (!Mathf.Approximately(statOffsetFromList, 0.0f)) {
                stringBuilder ??= new StringBuilder();
                stringBuilder.AppendLine(
                    whitespace + " - " +
                    weaponTraitDef.LabelCap + ": " +
                    stat.Worker.ValueToString(statOffsetFromList, false, ToStringNumberSense.Offset));
            }

            // factor
            var statFactorFromList = weaponTraitDef.statFactors.GetStatFactorFromList(stat);
            if (!Mathf.Approximately(statFactorFromList, 1f)) {
                stringBuilder ??= new StringBuilder();
                stringBuilder.AppendLine(
                    whitespace + "- " +
                    weaponTraitDef.LabelCap + ": " +
                    stat.Worker.ValueToString(statFactorFromList, false, ToStringNumberSense.Factor));
            }
        }

        if (stringBuilder == null) return;

        sb.AppendLine(whitespace + "CWF_UI_WeaponModules".Translate() + ":");
        sb.Append(stringBuilder);
    }

    public override string? CompInspectStringExtra() {
        if (_installedTraits.IsNullOrEmpty()) return null;

        return "CWF_UI_WeaponModules".Translate() + ": " + Traits
            .Select(traitDef => traitDef.label).ToCommaList()
            .CapitalizeFirst();
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats() {
        if (_installedTraits.IsNullOrEmpty()) yield break;

        var sb = new StringBuilder();
        sb.AppendLine("CWF_UI_WeaponModules_Desc".Translate());
        sb.AppendLine();

        var traitsList = Traits.ToList();
        for (var i = 0; i < traitsList.Count; i++) {
            var trait = traitsList[i];
            sb.AppendLine(trait.LabelCap.Colorize(ColorLibrary.Green));
            sb.AppendLine(trait.description);

            var effectLines = TraitModuleDatabase.GetTraitEffectLines(trait);
            if (!effectLines.IsNullOrEmpty()) {
                sb.AppendLine(effectLines.ToLineList());
            }

            if (i < traitsList.Count - 1) sb.AppendLine();
        }

        yield return new StatDrawEntry(
            parent.def.IsMeleeWeapon ? StatCategoryDefOf.Weapon_Melee : StatCategoryDefOf.Weapon_Ranged,
            "CWF_UI_WeaponModules".Translate(),
            traitsList.Select(x => x.label).ToCommaList().CapitalizeFirst(),
            sb.ToString(),
            1105
        );
    }

    // === Callback ===
    public override void PostPostMake() {
        base.PostPostMake();
        InitializeTraits();
        RecalculateAvailableParts();
    }

    public override void PostExposeData() {
        base.PostExposeData();
        Scribe_Collections.Look(ref _installedTraits, "installedParts", LookMode.Value, LookMode.Def);

        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;

        _installedTraits ??= new Dictionary<Part, WeaponTraitDef>(); // for legacy saves
        RecalculateAvailableParts();
        SetupAbility(true);
    }

    // === Gizmos ===
    public override IEnumerable<Gizmo> CompGetGizmosExtra() {
        foreach (var g in base.CompGetGizmosExtra()) {
            yield return g;
        }

        if (parent.IsForbidden(Faction.OfPlayer)) yield break;

        yield return new Command_Action {
            defaultLabel = "CWF_UI_WeaponPanel".Translate(),
            defaultDesc = "CWF_UI_WeaponPanelDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("CustomizeWeapon/Gizmos/Panel"),
            action = () => { Find.WindowStack.Add(new CustomizeWeaponWindow(parent)); }
        };

        if (!Prefs.DevMode) yield break;
        var addTraitGizmo = new Command_Action {
            defaultLabel = "Dev: + Add Trait",
            action = () => {
                var availableTraits = DefDatabase<WeaponTraitDef>.AllDefs
                    .Where(traitDef => {
                        if (!traitDef.TryGetPart(out var part)) return false;
                        if (_installedTraits.ContainsKey(part)) return false;

                        return traitDef.TryGetModuleDef(out var moduleDef) &&
                               TraitModuleDatabase.IsModuleCompatibleWithWeapon(moduleDef, parent.def);
                    })
                    .ToList();

                if (availableTraits.IsNullOrEmpty()) {
                    Messages.Message("Debug: No available traits to install.", MessageTypeDefOf.NeutralEvent);
                    return;
                }

                var options = new List<FloatMenuOption>();

                foreach (var traitDef in availableTraits) {
                    var localTraitDef = traitDef;

                    var option = new FloatMenuOption(
                        localTraitDef.LabelCap,
                        () => {
                            if (!localTraitDef.TryGetPart(out var part)) return;
                            InstallTrait(part, localTraitDef);
                        }
                    );
                    options.Add(option);
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
        yield return addTraitGizmo;

        var removeTraitGizmo = new Command_Action {
            defaultLabel = "Dev: - Remove Trait",
            action = () => {
                var options = new List<FloatMenuOption>();

                var availableTraits = Traits;

                foreach (var traitDef in availableTraits) {
                    var option = new FloatMenuOption(
                        traitDef.LabelCap,
                        () => {
                            if (!traitDef.TryGetPart(out var part)) return;
                            UninstallTrait(part);
                        }
                    );
                    options.Add(option);
                }

                if (options.Count == 0) {
                    options.Add((new FloatMenuOption("Nothing to remove", null)));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
        yield return removeTraitGizmo;
    }

    public IEnumerable<Gizmo> GetWornGizmosExtra(Pawn owner) {
        // harmony patched
        if (owner.Faction == Faction.OfPlayer) {
            yield return new Command_Action {
                defaultLabel = "CWF_UI_WeaponPanel".Translate(),
                defaultDesc = "CWF_UI_WeaponPanelDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("CustomizeWeapon/Gizmos/Panel"),
                action = () => { Find.WindowStack.Add(new CustomizeWeaponWindow(parent)); }
            };
        }
    }

    // Public Helper
    public WeaponTraitDef? GetInstalledTraitFor(Part part) {
        _installedTraits.TryGetValue(part, out var traitDef);
        return traitDef;
    }

    // === Private Helper ===
    private void InitializeTraits() {
        var defaultTraitsList = Props.defaultWeaponTraitDefs;
        if (defaultTraitsList.IsNullOrEmpty()) return;

        foreach (var traitDef in defaultTraitsList) {
            if (traitDef.TryGetPart(out var part)) {
                // Found the corresponding slot; now check whether the slot is already occupied (conflict check).
                if (_installedTraits.TryGetValue(part, out var existingTrait)) {
                    Log.Error($"[CWF] Initialization Error for {parent.def.defName}: " +
                              $"Both '{traitDef.defName}' and '{existingTrait.defName}' are configured as default traits for the same part slot '{part}'. " +
                              $"Ignoring the latter: '{traitDef.defName}'.");
                } else {
                    // No conflict—install this default trait into the corresponding slot.
                    _installedTraits.Add(part, traitDef);
                }
            } else {
                // If no corresponding module definition is found, skip it.
                Log.Warning($"[CWF] Initialization Warning for {parent.def.defName}: " +
                            $"Default trait '{traitDef.defName}' has no corresponding TraitModule with a Part defined. It will be ignored.");
            }
        }
    }

    private void OnTraitsChanged() {
        RecalculateAvailableParts();
        SetupAbility(false);
        ClearAllCaches();

        // graphic dirty
        if (!parent.TryGetComp<CompDynamicGraphic>(out var compDynamicGraphic)) return;
        compDynamicGraphic.Notify_GraphicDirty();
        if (parent.Map != null) {
            // ground graphic dirty
            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
        } else if (parent.ParentHolder is Pawn_EquipmentTracker { pawn: not null } equipmentTracker) {
            // equipment graphic dirty
            equipmentTracker.pawn.Drawer.renderer.SetAllGraphicsDirty();
        }
    }

    private void RecalculateAvailableParts() {
        _availableParts = new HashSet<Part>(Props.supportParts);

        foreach (var traitDef in Traits) {
            if (!traitDef.TryGetModuleDef(out var moduleDef)) continue;

            var modifiers = moduleDef.GetModExtension<TraitModuleExtension>()?.conditionalPartModifiers;
            if (modifiers == null) continue;

            foreach (var rule in modifiers) {
                if (rule.matcher == null || !rule.matcher.IsMatch(parent.def)) continue;

                if (!rule.enablesParts.IsNullOrEmpty()) {
                    _availableParts.UnionWith(rule.enablesParts);
                }

                if (!rule.disablesParts.IsNullOrEmpty()) {
                    _availableParts.ExceptWith(rule.disablesParts);
                }
            }
        }

        // temporary old save check mechanism
        foreach (var installedPart in _installedTraits.Keys) {
            if (_availableParts.Contains(installedPart)) continue;
            Log.Warning($"[CWF] Part '{installedPart}' on weapon '{parent.LabelCap}' is no longer available.");
        }
    }

    private void SetupAbility(bool isPostLoad) {
        var abilityProvider = parent.TryGetComp<CompAbilityProvider>();
        if (abilityProvider == null) return;

        var propsList = Traits
            .Where(trait => trait.abilityProps != null)
            .Select(trait => trait.abilityProps)
            .ToList();

        abilityProvider.SetOrUpdateAbilities(propsList, isPostLoad);
    }

    private void ClearAllCaches() {
        // === Stats ===
        foreach (var statDef in DefDatabase<StatDef>.AllDefs) {
            statDef.Worker.ClearCacheForThing(parent);
        }

        // === verbs ===
        var verb = parent.TryGetComp<CompEquippableAbilityReloadable>()?.PrimaryVerb ??
                   parent.TryGetComp<CompEquippable>()?.PrimaryVerb;
        if (verb == null) return;

        AccessTools.Field(typeof(Verb), "cachedBurstShotCount").SetValue(verb, null);
        AccessTools.Field(typeof(Verb), "cachedTicksBetweenBurstShots").SetValue(verb, null);
    }
}