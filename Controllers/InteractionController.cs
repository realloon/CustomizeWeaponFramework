using RimWorld;
using Verse;
using Verse.Sound;
using CWF.Extensions;

namespace CWF.Controllers;

public class InteractionController(Thing weapon) {
    private readonly CompDynamicTraits _compDynamicTraits = weapon.TryGetComp<CompDynamicTraits>();

    private readonly List<WeaponTraitDef> _stagedUninstalls = [];

    public event Action OnDataChanged = delegate { };

    private string FailureReason => weapon.ParentHolder is Pawn
        ? "CWF_UI_NoCompatiblePartsInInventory".Translate()
        : "CWF_UI_NoCompatiblePartsOnMap".Translate();

    /// <summary>
    /// Opens a float-menu for the clicked slot.  
    /// If installedTrait is null, lists traits to install; otherwise offers to uninstall the installed trait.
    /// </summary>
    public void HandleSlotClick(Part part, WeaponTraitDef? installedTrait) {
        var options = new List<FloatMenuOption>();

        if (installedTrait == null) {
            BuildInstallOptions(part, options);
        } else {
            BuildUninstallOption(part, options, installedTrait);
        }

        if (Enumerable.Any(options)) {
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private void BuildInstallOptions(Part part, List<FloatMenuOption> options) {
        var installCandidates = new Dictionary<WeaponTraitDef, ThingDef>();

        var compatibleModuleDefs = new HashSet<ThingDef>(GetCompatibleModuleDefsFor(part));

        var ownerPawn = weapon.ParentHolder switch {
            Pawn_EquipmentTracker equipment => equipment.pawn,
            Pawn_InventoryTracker inventory => inventory.pawn,
            _ => null
        };

        // from inventory or map
        if (compatibleModuleDefs.Any()) {
            var searchScope = ownerPawn != null
                ? ownerPawn.inventory.innerContainer
                : weapon.Map?.listerThings.AllThings ?? Enumerable.Empty<Thing>();

            var availableModules = searchScope.Where(t =>
                compatibleModuleDefs.Contains(t.def) &&
                (ownerPawn != null || !t.IsForbidden(Faction.OfPlayer))
            );

            foreach (var module in availableModules) {
                var trait = module.def.GetModExtension<TraitModuleExtension>().weaponTraitDef;
                installCandidates.TryAdd(trait, module.def);
            }
        }

        // from stack
        var stagedCompatibleTraits = _stagedUninstalls
            .Where(trait => trait.TryGetPart(out var p) && p == part);
        foreach (var trait in stagedCompatibleTraits) {
            if (trait.TryGetModuleDef(out var moduleDef)) {
                installCandidates.TryAdd(trait, moduleDef);
            }
        }

        if (!installCandidates.Any()) {
            options.Add(new FloatMenuOption(FailureReason, null));
            return;
        }

        foreach (var (traitToInstall, _) in installCandidates) {
            var installAction = CreateInstallAction(part, traitToInstall);
            options.Add(new FloatMenuOption(traitToInstall.LabelCap, installAction));
        }
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
                            if (dependencyTrait.TryGetPart(out var dependencyPart)) {
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
        _stagedUninstalls.Remove(traitToInstall);

        _compDynamicTraits.InstallTrait(part, traitToInstall);
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        OnDataChanged();
    }

    private void DoUninstall(Part part) {
        var traitToUninstall = _compDynamicTraits.GetInstalledTraitFor(part);
        if (traitToUninstall != null) {
            _stagedUninstalls.Add(traitToUninstall);
        }

        _compDynamicTraits.UninstallTrait(part);
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        OnDataChanged();
    }

    #region Helpers

    private Action CreateInstallAction(Part part, WeaponTraitDef traitToInstall) {
        return () => {
            if (!traitToInstall.TryGetModuleDef(out var moduleDef)) {
                DoInstall(part, traitToInstall);
                return;
            }

            var analysis = AnalyzeInstallConflict(moduleDef);
            if (!analysis.HasConflict) {
                DoInstall(part, traitToInstall);
                return;
            }

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
                        if (conflictTrait.TryGetPart(out var conflictPart)) {
                            DoUninstall(conflictPart);
                        }
                    }

                    DoInstall(part, traitToInstall);
                }
            );
        };
    }

    private ConflictAnalysisResult AnalyzeInstallConflict(ThingDef moduleToInstall) {
        var result = new ConflictAnalysisResult();
        var ext = moduleToInstall.GetModExtension<TraitModuleExtension>();
        if (ext?.conditionalPartModifiers == null) return result;

        var partsToDisable = new HashSet<Part>();
        foreach (var rule in ext.conditionalPartModifiers) {
            if (rule.matcher != null && rule.matcher.IsMatch(weapon.def) && !rule.disablesParts.IsNullOrEmpty()) {
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
            if (futureAvailableParts.Contains(part)) continue;

            result.ModulesToRemove.Add(trait);
        }

        return result;
    }

    private HashSet<Part> CalculateFutureAvailableParts(IEnumerable<WeaponTraitDef> futureTraits) {
        if (weapon.TryGetComp<CompDynamicTraits>()?.props is not CompProperties_DynamicTraits props) return [];

        var availableParts = new HashSet<Part>(props.supportParts);

        foreach (var traitDef in futureTraits) {
            if (!traitDef.TryGetModuleDef(out var moduleDef)) continue;

            var ext = moduleDef.GetModExtension<TraitModuleExtension>();
            if (ext?.conditionalPartModifiers == null) continue;

            foreach (var rule in ext.conditionalPartModifiers) {
                if (rule.matcher == null || !rule.matcher.IsMatch(weapon.def)) continue;

                if (!rule.enablesParts.IsNullOrEmpty()) {
                    availableParts.UnionWith(rule.enablesParts);
                }

                if (!rule.disablesParts.IsNullOrEmpty()) {
                    availableParts.ExceptWith(rule.disablesParts);
                }
            }
        }

        return availableParts;
    }

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
            .Where(moduleDef => TraitModuleDatabase.IsModuleCompatibleWithWeapon(moduleDef, weapon.def));
    }

    #endregion
}