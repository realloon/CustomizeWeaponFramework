using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CWF;

public class JobDriver_ModifyWeaponSelf : JobDriver {
    private Thing Weapon => TargetA.Thing;

    private ModificationData ModData => (job.source as JobGiver_ModifyWeapon)?.ModDataList.FirstOrFallback();

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
        modifyToil.defaultCompleteMode = ToilCompleteMode.Delay;

        modifyToil.initAction = () => {
            // callback: do something at init
        };
        modifyToil.tickAction = () => {
            // callback: do something in the process
        };

        // finished progress
        modifyToil.AddFinishAction(() => {
            var comp = Weapon.TryGetComp<CompDynamicTraits>();
            if (comp == null) return;

            if (ModData.Type == ModificationType.Install) {
                DoInstall(comp);
            } else {
                DoUninstall(comp);
            }
        });

        yield return modifyToil;
    }

    // === Helper ===
    private void DoInstall(CompDynamicTraits comp) {
        var moduleToUse = pawn.inventory.innerContainer.FirstOrDefault(t => t.def == ModData.ModuleDef);
        if (moduleToUse != null) {
            comp.InstallTrait(ModData.Part, ModData.Trait);
            moduleToUse.Destroy();
            SoundDefOf.Replant_Complete.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
        } else {
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
        }
    }

    private void DoUninstall(CompDynamicTraits comp) {
        comp.UninstallTrait(ModData.Part);
        var moduleThing = ThingMaker.MakeThing(ModData.ModuleDef);
        if (!pawn.inventory.innerContainer.TryAdd(moduleThing)) {
            GenPlace.TryPlaceThing(moduleThing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
        }

        SoundDefOf.Replant_Complete.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
    }
}