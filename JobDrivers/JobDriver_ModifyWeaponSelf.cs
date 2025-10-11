using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CWF;

public class JobDriver_ModifyWeaponSelf : JobDriver {
    private Thing Weapon => TargetA.Thing;

    private ModificationData? ModData => (job.source as JobGiver_ModifyWeapon)?.ModDataList.FirstOrFallback();

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils() {
        // safe check
        if (ModData == null) {
            Log.Error("[CWF] JobDriver_ModifyWeaponSelf started with null ModData. Aborting.");
            yield break; // end job
        }

        // wait and show progress
        var modifyToil = Toils_General.Wait(60);
        modifyToil.WithProgressBarToilDelay(TargetIndex.A);

        modifyToil.AddEndCondition(() => {
            if (ModData.Type != ModificationType.Install) return JobCondition.Ongoing;

            var moduleToUse = pawn.inventory.innerContainer.FirstOrDefault(t => t.def == ModData.ModuleDef);

            return moduleToUse != null ? JobCondition.Ongoing : JobCondition.Incompletable;
        });

        // modifyToil.initAction = () => {
        //     // callback: do something at init
        // };
        // modifyToil.tickAction = () => {
        //     // callback: do something in the process
        // };

        // finished progress
        modifyToil.AddFinishAction(() => {
            if (ended) return;

            if (!Weapon.TryGetComp<CompDynamicTraits>(out var compDynamicTraits)) return;

            if (ModData.Type == ModificationType.Install) {
                DoInstall(compDynamicTraits, ModData);
            } else {
                DoUninstall(compDynamicTraits, ModData);
            }
        });

        yield return modifyToil;
    }

    // === Helper ===
    private void DoInstall(CompDynamicTraits comp, ModificationData modData) {
        var moduleToUse = pawn.inventory.innerContainer.FirstOrDefault(t => t.def == modData.ModuleDef);
        if (moduleToUse != null) {
            comp.InstallTrait(modData.Part, modData.Trait);
            moduleToUse.SplitOff(1).Destroy();
            SoundDefOf.Replant_Complete.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
        } else {
            Log.Error($"[CWF] '{modData.ModuleDef.defName}' missing in FinishAction despite passing EndCondition.");
        }
    }

    private void DoUninstall(CompDynamicTraits comp, ModificationData modData) {
        comp.UninstallTrait(modData.Part);
        var moduleThing = ThingMaker.MakeThing(modData.ModuleDef);
        if (!pawn.inventory.innerContainer.TryAdd(moduleThing)) {
            GenPlace.TryPlaceThing(moduleThing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
        }

        SoundDefOf.Replant_Complete.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
    }
}