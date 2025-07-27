using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CustomizeWeapon.Controllers;

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

        var ownerPawn = _weapon.ParentHolder switch {
            Pawn_EquipmentTracker equipment => equipment.pawn,
            Pawn_InventoryTracker inventory => inventory.pawn,
            _ => null
        };

        if (installedTrait == null) {
            Install();
        } else {
            Uninstall();
        }

        if (options.Any()) Find.WindowStack.Add(new FloatMenu(options));

        return;

        // functions
        void Install() {
            var searchScope = ownerPawn != null
                ? ownerPawn.inventory.innerContainer
                : _weapon.Map?.listerThings.AllThings ?? Enumerable.Empty<Thing>();

            var availableModules = searchScope.Where(t => {
                var ext = t.def.GetModExtension<TraitModuleExtension>();
                if (ext == null || ext.part != part) return false;
                return ownerPawn != null || !t.IsForbidden(Faction.OfPlayer);
            });

            foreach (var moduleThing in availableModules) {
                var ext = moduleThing.def.GetModExtension<TraitModuleExtension>();
                var traitToInstall = ext.weaponTraitDef;

                options.Add(new FloatMenuOption(moduleThing.LabelCap, InstallAction));
                continue;

                void InstallAction() {
                    _compDynamicTraits.InstallTrait(part, traitToInstall);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    OnDataChanged?.Invoke();
                }
            }

            if (options.Any()) return;

            var reason = ownerPawn != null
                ? "CWF_UI_NoCompatiblePartsInInventory".Translate()
                : "CWF_UI_NoCompatiblePartsOnMap".Translate();
            options.Add(new FloatMenuOption(reason, null));
        }

        void Uninstall() {
            options.Add(new FloatMenuOption("CWF_UI_Uninstall".Translate() + installedTrait.LabelCap, UninstallAction));
            return;

            void UninstallAction() {
                _compDynamicTraits.UninstallTrait(part);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                OnDataChanged?.Invoke();
            }
        }
    }
}