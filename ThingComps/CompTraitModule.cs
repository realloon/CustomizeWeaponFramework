using RimWorld;
using Verse;
using Verse.AI;

namespace CWF;

public class CompTraitModule : ThingComp {
    // private CompProperties_TraitModule Props => (CompProperties_TraitModule)props;

    // private WeaponTraitDef _cachedTraitDef;
    // private bool _isTraitDefCached;

    // private WeaponTraitDef TraitDef {
    //     get {
    //         if (_isTraitDefCached) return _cachedTraitDef;
    //
    //         _cachedTraitDef = parent.def.GetModExtension<TraitModuleExtension>()?.weaponTraitDef;
    //         _isTraitDefCached = true;
    //         return _cachedTraitDef;
    //     }
    // }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn) {
        foreach (var option in base.CompFloatMenuOptions(selPawn)) {
            yield return option;
        }

        if (parent.IsForbidden(selPawn)) yield break;
        if (!selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) yield break;

        if (!selPawn.CanReserveAndReach(parent, PathEndMode.ClosestTouch, Danger.Deadly)) {
            yield return new FloatMenuOption("CannotReach".Translate(parent.LabelCap), null);
            yield break;
        }

        yield return new FloatMenuOption(
            "CWF_UI_PickUp".Translate(parent.Named("MODULE")),
            () => {
                var job = JobMaker.MakeJob(JobDefOf.TakeInventory, parent);
                job.count = 1;
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        );
    }
}