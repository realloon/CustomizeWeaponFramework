using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CWF;

public class JobDriver_ModifyWeaponHaul : JobDriver {
    private const TargetIndex WeaponInd = TargetIndex.A;
    private const TargetIndex ModuleToHaulInd = TargetIndex.B;
    private const int TicksPerModification = 60;

    private Thing Weapon => job.GetTarget(WeaponInd).Thing;
    private List<ModificationData> ModDataList => (job.source as JobGiver_ModifyWeapon)?.ModDataList;

    public override bool TryMakePreToilReservations(bool errorOnFailed) {
        // reserve weapon
        if (!pawn.Reserve(Weapon, job, 1, -1, null, errorOnFailed)) {
            return false;
        }

        // no modules to haul.
        if (job.targetQueueB.NullOrEmpty()) {
            return true;
        }

        var succeedReserved = job.targetQueueB
            .Where(target => pawn.Reserve(target.Thing, job, 1, -1, null, errorOnFailed))
            .ToList();

        // replace queue with succeed reserved modules.
        job.targetQueueB = Enumerable.Any(succeedReserved)
            ? succeedReserved
            : null;

        return true; // always succeed while holding a weapon.
    }

    protected override IEnumerable<Toil> MakeNewToils() {
        // === Phase 1: Preparation ===
        var startModificationPhase = Toils_General.Label();
        yield return Toils_Jump.JumpIf(startModificationPhase, () => job.targetQueueB.NullOrEmpty());

        // === Phase 2: Loop-Carry all required modules ===
        var haulLoop = Toils_General.Label();
        yield return haulLoop;

        yield return Toils_JobTransforms.ExtractNextTargetFromQueue(ModuleToHaulInd);

        yield return Toils_Goto
            .GotoThing(ModuleToHaulInd, PathEndMode.ClosestTouch)
            .FailOnDespawnedNullOrForbidden(ModuleToHaulInd);

        var customCarryToil = new Toil {
            initAction = () => {
                var actor = pawn;
                var thingToCarry = job.GetTarget(ModuleToHaulInd).Thing;

                if (thingToCarry == null || thingToCarry.Destroyed || thingToCarry.stackCount <= 0) {
                    // skip uncatchable modules.
                    actor.jobs.curDriver.ReadyForNextToil();
                    return;
                }

                actor.carryTracker.TryStartCarry(thingToCarry, 1);
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return customCarryToil;

        var moveToInventory = new Toil {
            initAction = () => {
                var carriedThing = pawn.carryTracker.CarriedThing;
                if (carriedThing != null) {
                    pawn.inventory.innerContainer.TryAddOrTransfer(carriedThing);
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return moveToInventory;

        yield return Toils_Jump.JumpIfHaveTargetInQueue(ModuleToHaulInd, haulLoop);

        // === Phase 3: Perform Modification ===
        yield return startModificationPhase;
        yield return Toils_Goto.GotoThing(WeaponInd, PathEndMode.Touch);

        var finalToil = Toils_General.WaitWith(WeaponInd, TicksPerModification * (ModDataList?.Count ?? 1), true, true);
        finalToil.FailOnCannotTouch(WeaponInd, PathEndMode.Touch);

        finalToil.AddFinishAction(() => {
            var comp = Weapon.TryGetComp<CompDynamicTraits>();
            if (comp == null || ModDataList == null) return;

            foreach (var modData in ModDataList) {
                if (modData.Type == ModificationType.Install) {
                    DoInstall(comp, modData);
                } else {
                    DoUninstall(comp, modData);
                }
            }

            Messages.Message("CWF_Message_ModificationComplete"
                    .Translate(pawn.Named("PAWN"), Weapon.Named("WEAPON")),
                new LookTargets(pawn, Weapon), MessageTypeDefOf.PositiveEvent);

            SoundDefOf.Replant_Complete.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
        });

        yield return finalToil;
    }

    // === Helper ===
    private void DoInstall(CompDynamicTraits comp, ModificationData modData) {
        var moduleToUse = pawn.inventory.innerContainer.FirstOrDefault(t => t.def == modData.ModuleDef);
        if (moduleToUse == null) {
            Log.Warning(
                $"[CWF] Pawn {pawn.LabelShort} planned to install {modData.ModuleDef.label} but could not find it in inventory at the last moment.");
            return; // fail silently, proceed to the next
        }

        comp.InstallTrait(modData.Part, modData.Trait);
        moduleToUse.Destroy();
    }

    private void DoUninstall(CompDynamicTraits comp, ModificationData modData) {
        comp.UninstallTrait(modData.Part);
        var moduleThing = ThingMaker.MakeThing(modData.ModuleDef);
        GenPlace.TryPlaceThing(moduleThing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
    }
}