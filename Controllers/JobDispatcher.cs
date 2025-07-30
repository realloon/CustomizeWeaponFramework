using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CustomizeWeapon.Controllers;

public class JobDispatcher {
    private readonly Thing _weapon;
    private readonly CompDynamicTraits _compDynamicTraits;
    private readonly Dictionary<Part, WeaponTraitDef> _initialTraitsState;

    public JobDispatcher(Thing weapon) {
        _weapon = weapon;
        _compDynamicTraits = weapon.TryGetComp<CompDynamicTraits>();
        _initialTraitsState = _compDynamicTraits?.GetInstalledTraits() ?? new Dictionary<Part, WeaponTraitDef>();
    }

    public void CommitChangesAndDispatchJobs() {
        var finalTraitsState = _compDynamicTraits.GetInstalledTraits();
        _compDynamicTraits.SetInstalledTraits(_initialTraitsState); // revert traits state, before commit

        var netChanges = CalculateNetChanges(_initialTraitsState, finalTraitsState);
        if (!netChanges.Any()) return;

        var ownerPawn = _weapon.ParentHolder switch {
            Pawn_EquipmentTracker equipment => equipment.pawn,
            Pawn_InventoryTracker inventory => inventory.pawn,
            _ => null
        };

        if (ownerPawn != null) {
            // Equip
            DispatchFieldModificationJobs(ownerPawn, netChanges);
        } else {
            // Ground
            DispatchHaulModificationJob(netChanges);
        }
    }

    // === Helper ===
    private void DispatchFieldModificationJobs(Pawn ownerPawn, List<ModificationData> netChanges) {
        foreach (var change in netChanges) {
            var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("CWF_ModifyWeaponSelf"), _weapon);
            job.source = new JobGiver_ModifyWeapon { ModDataList = new List<ModificationData> { change } };
            ownerPawn.jobs.jobQueue.EnqueueLast(job, JobTag.Misc);
        }

        if (ownerPawn.CurJob != null) {
            ownerPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
    }

    private void DispatchHaulModificationJob(List<ModificationData> netChanges) {
        var bestPawn = FindBestPawnForJob(_weapon.Position, _weapon.Map);
        if (bestPawn == null) {
            Messages.Message("CWF_Message_NoColonistToModifyWeapon".Translate(), MessageTypeDefOf.NeutralEvent, false);
            return;
        }

        var modulesToHaul = new List<Thing>();
        var installChanges = netChanges
            .Where(c => c.Type == ModificationType.Install)
            .ToList();
        foreach (var change in installChanges) {
            var module = FindBestAvailableModuleFor(change, bestPawn);
            if (module != null) {
                modulesToHaul.Add(module);
            } else {
                Messages.Message(
                    "CWF_Message_CannotFindModuleForModification".Translate(change.ModuleDef.label),
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
        }

        // create a big job merged all modification
        var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("CWF_ModifyWeaponHaul"), _weapon);

        // fill queue only when it needs haul
        if (modulesToHaul.Any()) {
            job.targetQueueB = modulesToHaul.Select(t => new LocalTargetInfo(t)).ToList();
        }

        // Package all modification data into the job source
        job.source = new JobGiver_ModifyWeapon { ModDataList = netChanges };

        bestPawn.jobs.jobQueue.EnqueueLast(job, JobTag.Misc);

        // gracefully end the current job.
        if (bestPawn.CurJob != null && bestPawn.CurJob.def != JobDefOf.Wait_Wander) {
            bestPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        Messages.Message(
            "CWF_Message_ModificationJobDispatched".Translate(bestPawn.Named("PAWN"), _weapon.Named("WEAPON")),
            new LookTargets(bestPawn, _weapon), MessageTypeDefOf.PositiveEvent);
    }

    private static List<ModificationData> CalculateNetChanges(Dictionary<Part, WeaponTraitDef> initial,
        Dictionary<Part, WeaponTraitDef> final) {
        var changes = new List<ModificationData>();
        var allParts = System.Enum.GetValues(typeof(Part)).Cast<Part>();

        foreach (var part in allParts) {
            initial.TryGetValue(part, out var initialTrait);
            final.TryGetValue(part, out var finalTrait);
            if (initialTrait == finalTrait) continue;

            if (initialTrait != null) {
                changes.Add(new ModificationData {
                    Type = ModificationType.Uninstall, Part = part, Trait = initialTrait,
                    ModuleDef = CustomizeWeaponUtility.GetModuleDefFor(initialTrait)
                });
            }

            if (finalTrait != null) {
                changes.Add(new ModificationData {
                    Type = ModificationType.Install, Part = part, Trait = finalTrait,
                    ModuleDef = CustomizeWeaponUtility.GetModuleDefFor(finalTrait)
                });
            }
        }

        return changes.OrderBy(c => c.Type).ToList();
    }

    private Thing FindBestAvailableModuleFor(ModificationData change, Pawn pawn) {
        if (change.Type != ModificationType.Install || change.ModuleDef == null) return null;

        return GenClosest.ClosestThingReachable(
            _weapon.Position,
            _weapon.Map,
            ThingRequest.ForDef(change.ModuleDef),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            validator: t => !t.IsForbidden(pawn) && !t.IsBurning() && pawn.CanReserve(t)
        );
    }

    private static Pawn FindBestPawnForJob(IntVec3 jobLocation, Map map) {
        return map.mapPawns.FreeColonistsSpawned
            .Where(p => !p.Downed && !p.Drafted && p.workSettings.WorkIsActive(WorkTypeDefOf.Crafting) &&
                        p.health.capacities.CanBeAwake)
            .OrderBy(p => p.Position.DistanceToSquared(jobLocation))
            .FirstOrDefault();
    }
}