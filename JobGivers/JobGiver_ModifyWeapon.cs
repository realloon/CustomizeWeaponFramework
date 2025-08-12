using Verse;
using Verse.AI;

namespace CWF;

public class JobGiver_ModifyWeapon : ThinkNode_JobGiver, IExposable, ILoadReferenceable {
    public List<ModificationData> ModDataList; // for transfer of data
    private int _loadID = -1;

    // This method must be overridden, but will never be invoked via the AI tree.
    protected override Job TryGiveJob(Pawn pawn) => null;

    public void ExposeData() {
        Scribe_Collections.Look(ref ModDataList, "modDataList", LookMode.Deep);
    }

    public string GetUniqueLoadID() {
        if (_loadID == -1) {
            _loadID = Find.UniqueIDsManager.GetNextThingID();
        }

        return $"JobGiver_ModifyWeapon_{_loadID}";
    }
}