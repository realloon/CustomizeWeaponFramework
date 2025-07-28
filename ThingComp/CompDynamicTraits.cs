using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using Verse;

namespace CustomizeWeapon;

public class CompDynamicTraits : ThingComp {
    private CompProperties_DynamicTraits Props => (CompProperties_DynamicTraits)props;

    private Dictionary<Part, WeaponTraitDef> _installedTraits = new();

    public IReadOnlyCollection<WeaponTraitDef> Traits => _installedTraits.Values;

    public void InstallTrait(Part part, WeaponTraitDef traitDef) {
        if (_installedTraits.ContainsKey(part)) {
            Log.Error(
                $"[CWF] Trying to install part {traitDef.defName} into already occupied slot {part} on {parent.LabelCap}.");
            return;
        }

        _installedTraits[part] = traitDef;
        OnTraitsChanged();
    }

    public void UninstallTrait(Part part) {
        if (!_installedTraits.Remove(part)) return;

        OnTraitsChanged();
    }

    public void ClearTraits() {
        _installedTraits.Clear();

        OnTraitsChanged();
    }

    public WeaponTraitDef GetInstalledTraitFor(Part part) {
        _installedTraits.TryGetValue(part, out var traitDef);
        return traitDef;
    }

    /// <summary>
    /// Returns a copy of the dict containing all currently installed Traits.
    /// </summary>
    /// <returns>A new shallow-copied dict instance that contains every Part-to-Trait mapping.</returns>
    public Dictionary<Part, WeaponTraitDef> GetInstalledTraits() {
        return new Dictionary<Part, WeaponTraitDef>(_installedTraits);
    }

    /// <summary>
    /// Completely overwrites the currently installed Traits with a new dict.
    /// </summary>
    /// <param name="newTraitsDict">The new dict of Traits to set.</param>
    public void SetInstalledTraits(Dictionary<Part, WeaponTraitDef> newTraitsDict) {
        _installedTraits = newTraitsDict != null
            ? new Dictionary<Part, WeaponTraitDef>(newTraitsDict)
            : new Dictionary<Part, WeaponTraitDef>();

        OnTraitsChanged();
    }


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
        var stringBuilder = new StringBuilder();

        foreach (var weaponTraitDef in Traits) {
            // offset
            var statOffsetFromList = weaponTraitDef.statOffsets.GetStatOffsetFromList(stat);
            if (!Mathf.Approximately(statOffsetFromList, 0.0f)) {
                stringBuilder.AppendLine(
                    whitespace + " - " +
                    weaponTraitDef.LabelCap + ": " +
                    stat.Worker.ValueToString(statOffsetFromList, false, ToStringNumberSense.Offset));
            }

            // factor
            var statFactorFromList = weaponTraitDef.statFactors.GetStatFactorFromList(stat);
            if (!Mathf.Approximately(statFactorFromList, 1f)) {
                stringBuilder.AppendLine(
                    whitespace + "- " +
                    weaponTraitDef.LabelCap + ": " +
                    stat.Worker.ValueToString(statFactorFromList, false, ToStringNumberSense.Factor));
            }
        }

        if (stringBuilder.Length == 0) return;

        sb.AppendLine(whitespace + "StatsReport_WeaponTraits".Translate() + ":");
        sb.Append(stringBuilder);
    }

    public override string CompInspectStringExtra() {
        if (_installedTraits.NullOrEmpty()) return null;

        return "WeaponTraits".Translate() + ": " + Traits
            .Select(traitDef => traitDef.label).ToCommaList()
            .CapitalizeFirst();
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats() {
        if (_installedTraits.NullOrEmpty()) yield break;

        var sb = new StringBuilder();
        sb.AppendLine("Stat_ThingUniqueWeaponTrait_Desc".Translate());
        sb.AppendLine();

        var traitsList = Traits.ToList();
        for (var i = 0; i < traitsList.Count; i++) {
            var trait = traitsList[i];
            sb.AppendLine(trait.LabelCap.Colorize(ColorLibrary.Green));
            sb.AppendLine(trait.description);

            if (!trait.statOffsets.NullOrEmpty()) {
                sb.Append(trait.statOffsets.Select(x =>
                        $" - {x.stat.LabelCap}: {x.stat.Worker.ValueToString(x.value, false, ToStringNumberSense.Offset)}")
                    .ToLineList());
                sb.AppendLine();
            }

            if (!trait.statFactors.NullOrEmpty()) {
                sb.Append(trait.statFactors.Select(x =>
                        $" - {x.stat.LabelCap}: {x.stat.Worker.ValueToString(x.value, false, ToStringNumberSense.Factor)}")
                    .ToLineList());
                sb.AppendLine();
            }

            if (!Mathf.Approximately(trait.burstShotCountMultiplier, 1f)) {
                sb.AppendLine(
                    $" - {"StatsReport_BurstShotCountMultiplier".Translate()} {trait.burstShotCountMultiplier.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor)}");
            }

            if (!Mathf.Approximately(trait.burstShotSpeedMultiplier, 1f)) {
                sb.AppendLine(
                    $" - {"StatsReport_BurstShotSpeedMultiplier".Translate()} {trait.burstShotSpeedMultiplier.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor)}");
            }

            if (!Mathf.Approximately(trait.additionalStoppingPower, 0.0f)) {
                sb.AppendLine(
                    $" - {"StatsReport_AdditionalStoppingPower".Translate()} {trait.additionalStoppingPower.ToStringByStyle(ToStringStyle.FloatOne, ToStringNumberSense.Offset)}");
            }

            if (i < traitsList.Count - 1) sb.AppendLine();
        }

        yield return new StatDrawEntry(
            parent.def.IsMeleeWeapon ? StatCategoryDefOf.Weapon_Melee : StatCategoryDefOf.Weapon_Ranged,
            "Stat_ThingUniqueWeaponTrait_Label".Translate(),
            traitsList.Select(x => x.label).ToCommaList().CapitalizeFirst(),
            sb.ToString(),
            1105
        );
    }

    // === Callback ===
    public override void PostPostMake() {
        base.PostPostMake();
        InitializeTraits();
    }

    public override void PostExposeData() {
        base.PostExposeData();
        Scribe_Collections.Look(ref _installedTraits, "installedParts", LookMode.Value, LookMode.Def);

        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;

        _installedTraits ??= new Dictionary<Part, WeaponTraitDef>(); // for legacy saves
        SetupAbility(true);
    }

    // === Helper ===
    private void InitializeTraits() {
        var defaultTraitsList = Props.defaultWeaponTraitDefs;
        if (defaultTraitsList.NullOrEmpty()) return;

        foreach (var traitDef in defaultTraitsList) {
            if (CustomizeWeaponUtility.TryGetPartForTrait(traitDef, out var part)) {
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

    private void SetupAbility(bool isPostLoad) {
        var abilityComp = parent.TryGetComp<CompEquippableAbilityReloadable>();
        if (abilityComp == null) return;

        // only the first ability is applied
        var traitWithAbility = Traits.FirstOrDefault(trait => trait.abilityProps != null);
        if (traitWithAbility != null) {
            abilityComp.props = traitWithAbility.abilityProps;
        } else {
            var originalProps = parent.def.comps
                .FirstOrDefault(p => p is CompProperties_EquippableAbilityReloadable);
            abilityComp.props = originalProps;
        }

        if (!isPostLoad) {
            abilityComp.Notify_PropsChanged();
        }
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

        Helper.Reflect.Set(verb, "cachedBurstShotCount", null);
        Helper.Reflect.Set(verb, "cachedTicksBetweenBurstShots", null);
    }

    // A helper method to reduce redundancy
    private void OnTraitsChanged() {
        ClearAllCaches();
        SetupAbility(false);

        // graphic dirty
        var compDynamicGraphic = parent.TryGetComp<CompDynamicGraphic>();
        if (compDynamicGraphic == null) return;
        compDynamicGraphic.Notify_GraphicDirty();
        if (parent.Map != null) {
            // ground graphic dirty
            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
        } else if (parent.ParentHolder is Pawn_EquipmentTracker { pawn: not null } equipmentTracker) {
            // equipment graphic dirty
            equipmentTracker.pawn.Drawer.renderer.SetAllGraphicsDirty();
        }
    }

    // === Gizmos ===
    public override IEnumerable<Gizmo> CompGetGizmosExtra() {
        foreach (var g in base.CompGetGizmosExtra())
            yield return g;

        if (parent.IsForbidden(Faction.OfPlayer)) yield break;

        yield return new Command_Action {
            defaultLabel = "CWF_UI_WeaponPanel".Translate(),
            defaultDesc = "CWF_UI_WeaponPanelDesc".Translate(),
            action = () => { Find.WindowStack.Add(new CustomizeWeaponWindow(parent)); }
        };
    }

    public IEnumerable<Gizmo> GetWornGizmosExtra(Pawn owner) {
        if (owner != null && owner.Faction == Faction.OfPlayer) {
            yield return new Command_Action {
                defaultLabel = "CWF_UI_WeaponPanel".Translate(),
                defaultDesc = "CWF_UI_WeaponPanelDesc".Translate(),
                // icon = ...
                action = () => { Find.WindowStack.Add(new CustomizeWeaponWindow(parent)); }
            };
        }
    }
}