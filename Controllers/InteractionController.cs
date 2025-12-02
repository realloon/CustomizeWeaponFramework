using RimWorld;
using Verse;
using Verse.Sound;

namespace CWF.Controllers;

public class InteractionController(Thing weapon) {
    private readonly CompDynamicTraits _compDynamicTraits = weapon.TryGetComp<CompDynamicTraits>();

    private readonly List<WeaponTraitDef> _stagedUninstalls = [];

    public event Action OnDataChanged = delegate { };

    private string FailureReason => weapon.ParentHolder is Pawn_EquipmentTracker
        ? "CWF_UI_NoCompatibleModulesInInventory".Translate()
        : "CWF_UI_NoCompatibleModulesOnMap".Translate();

    /// <summary>
    /// Opens a float-menu for the clicked slot.  
    /// If installedTrait is null, lists traits to install; otherwise offers to uninstall the installed trait.
    /// </summary>
    public void HandleSlotClick(PartDef part, WeaponTraitDef? installedTrait) {
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

    private void BuildInstallOptions(PartDef part, List<FloatMenuOption> options) {
        // 获取所有兼容的配件定义
        var compatibleModuleDefs = GetCompatibleModuleDefsFor(part).ToList();
        
        if (!compatibleModuleDefs.Any()) {
            options.Add(new FloatMenuOption("CWF_UI_NoSupportedModules".Translate(), null));
            return;
        }

        var ownerPawn = weapon.ParentHolder switch {
            Pawn_EquipmentTracker equipment => equipment.pawn,
            Pawn_InventoryTracker inventory => inventory.pawn,
            _ => null
        };

        // 获取搜索范围（库存或地图）
        var searchScope = ownerPawn != null
            ? ownerPawn.inventory.innerContainer
            : weapon.Map?.listerThings.AllThings ?? Enumerable.Empty<Thing>();

        // 查找库存中已有的配件
        var availableModulesInStock = searchScope
            .Where(t => compatibleModuleDefs.Contains(t.def) &&
                       (ownerPawn != null || !t.IsForbidden(Faction.OfPlayer)))
            .GroupBy(t => t.def)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 处理暂存的卸载配件（这些可以直接安装，不需要检查库存）
        var stagedCompatibleTraits = _stagedUninstalls
            .Where(trait => trait.TryGetPart(out var p) && p == part)
            .ToHashSet();

        // 为所有兼容的配件创建选项
        foreach (var moduleDef in compatibleModuleDefs) {
            var ext = moduleDef.GetModExtension<TraitModuleExtension>();
            if (ext?.weaponTraitDef == null) continue;

            var traitToInstall = ext.weaponTraitDef;
            
            // 检查是否在暂存列表中（从卸载的配件，这些可以直接安装）
            var isStaged = stagedCompatibleTraits.Contains(traitToInstall);
            
            // 检查库存中是否有该配件
            var hasInStock = availableModulesInStock.ContainsKey(moduleDef);

            if (hasInStock || isStaged) {
                // 库存中有配件或暂存中有配件，正常显示并可以安装
                var installAction = CreateInstallAction(part, traitToInstall);
                options.Add(new FloatMenuOption(traitToInstall.LabelCap, installAction));
            } else {
                // 库存中没有配件，显示"库存不足，点击添加制作订单"
                var addBillAction = CreateAddBillAction(part, traitToInstall, moduleDef);
                var label = $"{traitToInstall.LabelCap} ({"CWF_UI_InsufficientStockClickToAddOrder".Translate()})";
                options.Add(new FloatMenuOption(label, addBillAction));
            }
        }

        // 如果没有可用选项，显示提示
        if (!options.Any()) {
            options.Add(new FloatMenuOption(FailureReason, null));
        }
    }


    private void BuildUninstallOption(PartDef part, List<FloatMenuOption> options, WeaponTraitDef installedTrait) {
        options.Add(new FloatMenuOption("CWF_UI_Uninstall".Translate(installedTrait.LabelCap), () => {
            var analysis = AnalyzeUninstallConflict(part);
            if (!analysis.HasConflict) {
                DoUninstall(part);
            } else {
                ShowConfirmationDialog(
                    "CWF_UI_ConfirmUninstallTitle".Translate(),
                    "CWF_UI_ConfirmUninstallBody".Translate(
                        installedTrait.LabelCap.Named("MODULE"),
                        analysis.ModulesToRemove
                            .Select(t => " - " + t.LabelCap.ToString()).ToLineList()
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

    private void DoInstall(PartDef part, WeaponTraitDef traitToInstall) {
        _stagedUninstalls.Remove(traitToInstall);

        _compDynamicTraits.InstallTrait(part, traitToInstall);
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        OnDataChanged();
    }

    private void DoUninstall(PartDef part) {
        var traitToUninstall = _compDynamicTraits.GetInstalledTraitFor(part);
        if (traitToUninstall != null) {
            _stagedUninstalls.Add(traitToUninstall);
        }

        _compDynamicTraits.UninstallTrait(part);
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        OnDataChanged();
    }

    #region Helpers

    private Action CreateInstallAction(PartDef part, WeaponTraitDef traitToInstall) {
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
                    analysis.ModulesToRemove
                        .Select(t => " - " + t.LabelCap.ToString()).ToLineList()
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

        var partsToDisable = new HashSet<PartDef>();
        foreach (var rule in ext.conditionalPartModifiers) {
            if (rule.matcher != null && rule.matcher.IsMatch(weapon.def)) {
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

    private ConflictAnalysisResult AnalyzeUninstallConflict(PartDef partToUninstall) {
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

    private HashSet<PartDef> CalculateFutureAvailableParts(IEnumerable<WeaponTraitDef> futureTraits) {
        if (weapon.TryGetComp<CompDynamicTraits>()?.props is not CompProperties_DynamicTraits props) return [];

        var availableParts = new HashSet<PartDef>(props.supportParts);

        foreach (var traitDef in futureTraits) {
            if (!traitDef.TryGetModuleDef(out var moduleDef)) continue;

            var ext = moduleDef.GetModExtension<TraitModuleExtension>();
            if (ext?.conditionalPartModifiers == null) continue;

            foreach (var rule in ext.conditionalPartModifiers) {
                if (rule.matcher == null || !rule.matcher.IsMatch(weapon.def)) continue;

                availableParts.UnionWith(rule.enablesParts);
                availableParts.ExceptWith(rule.disablesParts);
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

    private IEnumerable<ThingDef> GetCompatibleModuleDefsFor(PartDef part) {
        return ModuleDatabase.AllModuleDefs
            .Where(moduleDef => moduleDef.GetModExtension<TraitModuleExtension>()?.part == part)
            .Where(moduleDef => moduleDef.IsCompatibleWith(weapon.def));
    }

    /// <summary>
    /// 创建添加制作订单的Action
    /// </summary>
    private Action CreateAddBillAction(PartDef part, WeaponTraitDef traitDef, ThingDef moduleDef) {
        return () => {
            var weaponName = weapon.LabelCap;
            var map = weapon.Map ?? Find.CurrentMap;
            
            if (AttachmentCraftingUtility.AddCraftingBillForModule(moduleDef, weaponName, map)) {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        };
    }

    #endregion
}

public class ConflictAnalysisResult {
    public List<WeaponTraitDef> ModulesToRemove { get; } = [];

    public bool HasConflict => !ModulesToRemove.NullOrEmpty();
}