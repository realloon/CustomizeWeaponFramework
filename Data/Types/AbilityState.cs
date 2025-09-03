using RimWorld;
using Verse;

namespace CWF;

public class AbilityState : IExposable {
    public string? defName;
    public int remainingCharges;
    public int cooldownTicksRemaining;
    public int cooldownTicksTotal;

    public AbilityState() {
    }

    public AbilityState(Ability ability) {
        defName = ability.def.defName;
        remainingCharges = ability.RemainingCharges;
        cooldownTicksRemaining = ability.CooldownTicksRemaining;
        cooldownTicksTotal = ability.CooldownTicksTotal;
    }

    public void ExposeData() {
        Scribe_Values.Look(ref defName, "defName");
        Scribe_Values.Look(ref remainingCharges, "remainingCharges", -1);
        Scribe_Values.Look(ref cooldownTicksRemaining, "cooldownTicksRemaining");
        Scribe_Values.Look(ref cooldownTicksTotal, "cooldownTicksTotal");
    }
}