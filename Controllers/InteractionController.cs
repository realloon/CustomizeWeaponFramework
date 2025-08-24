using RimWorld;
using Verse;
using Verse.Sound;

namespace CWF.Controllers;

public class InteractionController {
    private readonly Thing _weapon;
    private readonly CompDynamicTraits _compDynamicTraits;
    public event Action OnDataChanged;

    public InteractionController(Thing weapon) {
        _weapon = weapon;
        _compDynamicTraits = weapon.TryGetComp<CompDynamicTraits>();
    }

    public void HandleSlotClick(Part part, WeaponTraitDef installedTrait) {
        var options = new List<FloatMenuOption>();

        if (installedTrait == null) {
            BuildInstallOptions(part, options);
        } else {
            BuildUninstallOption(part, options, installedTrait);
        }

        if (Enumerable.Any(options)) Find.WindowStack.Add(new FloatMenu(options));
    }

    private void BuildInstallOptions(Part part, List<FloatMenuOption> options) {
        var compatibleModuleDefs = new HashSet<ThingDef>(GetCompatibleModuleDefsFor(part));

        var ownerPawn = _weapon.ParentHolder switch {
            Pawn_EquipmentTracker equipment => equipment.pawn,
            Pawn_InventoryTracker inventory => inventory.pawn,
            _ => null
        };

        if (!Enumerable.Any(compatibleModuleDefs)) {
            options.Add(new FloatMenuOption(FailureReason(), null));
            return;
        }

        var searchScope = ownerPawn != null
            ? ownerPawn.inventory.innerContainer
            : _weapon.Map?.listerThings.AllThings ?? Enumerable.Empty<Thing>();

        var availableModules = searchScope.Where(t =>
            compatibleModuleDefs.Contains(t.def) &&
            (ownerPawn != null || !t.IsForbidden(Faction.OfPlayer))
        );

        var groupedModules = availableModules.GroupBy(t => t.def);

        foreach (var group in groupedModules) {
            var moduleDef = group.Key;
            var traitToInstall = moduleDef.GetModExtension<TraitModuleExtension>().weaponTraitDef;

            options.Add(new FloatMenuOption(traitToInstall.LabelCap, () => {
                var analysis = AnalyzeInstallConflict(moduleDef);
                if (!analysis.HasConflict) {
                    DoInstall(part, traitToInstall);
                } else {
                    ShowConfirmationDialog(
                        "CWF_UI_ConfirmInstallTitle".Translate(),
                        "CWF_UI_ConfirmInstallBody".Translate(
                            traitToInstall.LabelCap.Named("MODULE"),
                            string.Join("\n", analysis.ModulesToRemove
                                    .Select(t => " - " + t.LabelCap.ToString()))
                                .Named("CONFLICTS")
                        ),
                        () => {
                            foreach (var conflictTrait in analysis.ModulesToRemove) {
                                if (TraitModuleDatabase.TryGetPartForTrait(conflictTrait, out var conflictPart)) {
                                    DoUninstall(conflictPart);
                                }
                            }

                            DoInstall(part, traitToInstall);
                        }
                    );
                }
            }));
        }

        if (Enumerable.Any(options)) return;

        options.Add(new FloatMenuOption(FailureReason(), null));
        return;

        TaggedString FailureReason() => ownerPawn != null
            ? "CWF_UI_NoCompatiblePartsInInventory".Translate()
            : "CWF_UI_NoCompatiblePartsOnMap".Translate();
    }

    private void BuildUninstallOption(Part part, List<FloatMenuOption> options, WeaponTraitDef installedTrait) {
        options.Add(new FloatMenuOption("CWF_UI_Uninstall".Translate(installedTrait.LabelCap), () => {
            var analysis = AnalyzeUninstallConflict(part);
            if (!analysis.HasConflict) {
                DoUninstall(part);
            } else {
                ShowConfirmationDialog(
                    "CWF_UI_ConfirmUninstallTitle".Translate(),
                    "CWF_UI_ConfirmUninstallBody".Translate(
                        installedTrait.LabelCap.Named("MODULE"),
                        string.Join("\n", analysis.ModulesToRemove
                                .Select(t => " - " + t.LabelCap.ToString()))
                            .Named("DEPENDENCIES")
                    ),
                    () => {
                        foreach (var dependencyTrait in analysis.ModulesToRemove) {
                            if (TraitModuleDatabase.TryGetPartForTrait(dependencyTrait, out var dependencyPart)) {
                                DoUninstall(dependencyPart);
                            }
                        }

                        DoUninstall(part);
                    }
                );
            }
        }));
    }

    private void DoInstall(Part part, WeaponTraitDef traitToInstall) {
        _compDynamicTraits.InstallTrait(part, traitToInstall);
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        OnDataChanged?.Invoke();
    }

    private void DoUninstall(Part part) {
        _compDynamicTraits.UninstallTrait(part);
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        OnDataChanged?.Invoke();
    }

    #region Helpers

    // === Consequence Analysis ===
    private ConflictAnalysisResult AnalyzeInstallConflict(ThingDef moduleToInstall) {
        var result = new ConflictAnalysisResult();
        var ext = moduleToInstall.GetModExtension<TraitModuleExtension>();
        if (ext?.conditionalPartModifiers == null) return result;

        var partsToDisable = new HashSet<Part>();
        foreach (var rule in ext.conditionalPartModifiers) {
            if (rule.matcher != null && rule.matcher.IsMatch(_weapon.def) && !rule.disablesParts.NullOrEmpty()) {
                partsToDisable.UnionWith(rule.disablesParts);
            }
        }

        foreach (var part in partsToDisable) {
            var conflictingTrait = _compDynamicTraits.GetInstalledTraitFor(part);
            if (conflictingTrait != null) {
                result.ModulesToRemove.Add(conflictingTrait);
            }
        }

        return result;
    }

    private ConflictAnalysisResult AnalyzeUninstallConflict(Part partToUninstall) {
        var result = new ConflictAnalysisResult();
        var currentTraits = _compDynamicTraits.InstalledTraits;

        if (!currentTraits.Remove(partToUninstall)) return result;

        var futureAvailableParts = CalculateFutureAvailableParts(currentTraits.Values);

        foreach (var (part, trait) in currentTraits) {
            if (!futureAvailableParts.Contains(part)) {
                result.ModulesToRemove.Add(trait);
            }
        }

        return result;
    }

    private HashSet<Part> CalculateFutureAvailableParts(IEnumerable<WeaponTraitDef> futureTraits) {
        var props = _weapon.TryGetComp<CompDynamicTraits>()?.props as CompProperties_DynamicTraits;
        if (props == null) return new HashSet<Part>();

        var availableParts = new HashSet<Part>(props.supportParts);

        foreach (var traitDef in futureTraits) {
            var ext = TraitModuleDatabase.GetModuleDefFor(traitDef)?.GetModExtension<TraitModuleExtension>();
            if (ext?.conditionalPartModifiers == null) continue;

            foreach (var rule in ext.conditionalPartModifiers) {
                if (rule.matcher == null || !rule.matcher.IsMatch(_weapon.def)) continue;

                if (!rule.enablesParts.NullOrEmpty()) {
                    availableParts.UnionWith(rule.enablesParts);
                }

                if (!rule.disablesParts.NullOrEmpty()) {
                    availableParts.ExceptWith(rule.disablesParts);
                }
            }
        }

        return availableParts;
    }

    // === UI & Compatibility ===
    private static void ShowConfirmationDialog(string title, string text, Action onConfirm) {
        var dialog = new Dialog_MessageBox(
            text,
            "Confirm".Translate(), onConfirm,
            "Cancel".Translate(), null,
            title,
            true, onConfirm
        );
        Find.WindowStack.Add(dialog);
    }

    private IEnumerable<ThingDef> GetCompatibleModuleDefsFor(Part part) {
        return TraitModuleDatabase.GetAllModuleDefs()
            .Where(moduleDef => moduleDef.GetModExtension<TraitModuleExtension>().part == part)
            .Where(moduleDef => TraitModuleDatabase.IsModuleCompatibleWithWeapon(moduleDef, _weapon.def));
    }

    #endregion
}