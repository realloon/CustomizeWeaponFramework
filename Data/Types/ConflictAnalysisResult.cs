using RimWorld;
using Verse;

namespace CWF;

public class ConflictAnalysisResult {
    public List<WeaponTraitDef> ModulesToRemove { get; } = [];

    public bool HasConflict => !ModulesToRemove.NullOrEmpty();
}