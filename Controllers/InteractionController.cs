using RimWorld;
using Verse;
using Verse.Sound;
using Find = Verse.Find;
using FloatMenu = Verse.FloatMenu;
using FloatMenuOption = Verse.FloatMenuOption;
using Thing = Verse.Thing;
using ThingDef = Verse.ThingDef;

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
            Install(part, options);
        } else {
            Uninstall(part, options, installedTrait);
        }

        if (Enumerable.Any(options)) Find.WindowStack.Add(new FloatMenu(options));
    }

    private void Install(Part part, List<FloatMenuOption> options) {
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
                _compDynamicTraits.InstallTrait(part, traitToInstall);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                OnDataChanged?.Invoke();
            }));
        }

        if (Enumerable.Any(options)) return;

        options.Add(new FloatMenuOption(FailureReason(), null));
        return;

        TaggedString FailureReason() => ownerPawn != null
            ? "CWF_UI_NoCompatiblePartsInInventory".Translate()
            : "CWF_UI_NoCompatiblePartsOnMap".Translate();
    }

    private void Uninstall(Part part, List<FloatMenuOption> options, WeaponTraitDef installedTrait) {
        options.Add(new FloatMenuOption("CWF_UI_Uninstall".Translate() + installedTrait.LabelCap, UninstallAction));
        return;

        void UninstallAction() {
            _compDynamicTraits.UninstallTrait(part);
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            OnDataChanged?.Invoke();
        }
    }

    // === Helper ===
    private IEnumerable<ThingDef> GetCompatibleModuleDefsFor(Part part) {
        return TraitModuleDatabase.GetAllModuleDefs()
            .Where(moduleDef => moduleDef.GetModExtension<TraitModuleExtension>().part == part)
            .Where(moduleDef => TraitModuleDatabase.IsModuleCompatibleWithWeapon(moduleDef, _weapon.def));
    }
}