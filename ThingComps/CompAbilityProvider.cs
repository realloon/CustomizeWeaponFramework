using HarmonyLib;
using RimWorld;
using Verse;

namespace CWF;

public class CompAbilityProvider : ThingComp {
    private Pawn _holder;

    private List<CompProperties_EquippableAbility> _abilityPropsToManage = new();

    private Dictionary<Ability, CompProperties_EquippableAbility> _managedAbilities = new();

    private List<AbilityState> _savedAbilityStates = new();

    public void SetOrUpdateAbilities(List<CompProperties_EquippableAbilityReloadable> newPropsList, bool isPostLoad) {
        _abilityPropsToManage = newPropsList
            .Cast<CompProperties_EquippableAbility>()
            .ToList();

        if (_holder is null) return;

        ApplyAbilityChanges(isPostLoad);
    }

    public void OnEquipped(Pawn pawn) {
        if (pawn is null) return;

        _holder = pawn;
        _managedAbilities = new Dictionary<Ability, CompProperties_EquippableAbility>();
        ApplyAbilityChanges(false);
    }

    public void OnUnequipped(Pawn pawn) {
        // todo
        if (_holder is null) return;

        foreach (var ability in _managedAbilities.Keys) {
            _holder.abilities.RemoveAbility(ability.def);
        }

        _holder.abilities.Notify_TemporaryAbilitiesChanged();

        _managedAbilities.Clear();
        _holder = null;
    }

    private void ApplyAbilityChanges(bool isPostLoad) {
        if (_holder is null) return;

        var propsToAdd = new List<CompProperties_EquippableAbility>(_abilityPropsToManage);
        var abilitiesToRemove = new List<Ability>();

        foreach (var kvp in _managedAbilities) {
            if (!_abilityPropsToManage.Contains(kvp.Value)) {
                abilitiesToRemove.Add(kvp.Key);
            } else {
                propsToAdd.Remove(kvp.Value);
            }
        }

        foreach (var ability in abilitiesToRemove) {
            _holder.abilities.RemoveAbility(ability.def);
            _managedAbilities.Remove(ability);
        }

        foreach (var abilityProps in propsToAdd) {
            _holder.abilities.GainAbility(abilityProps.abilityDef);
            var newAbility = _holder.abilities.GetAbility(abilityProps.abilityDef);
            if (newAbility is null) continue;

            _managedAbilities.Add(newAbility, abilityProps);

            if (abilityProps is not CompProperties_EquippableAbilityReloadable reloadableProps) continue;

            newAbility.maxCharges = reloadableProps.maxCharges;
            var savedState = _savedAbilityStates?
                .FirstOrDefault(s => s.defName == newAbility.def.defName);

            if (savedState is not null) {
                newAbility.RemainingCharges = savedState.remainingCharges;

                if (savedState.cooldownTicksRemaining > 0) {
                    var inCooldownField = AccessTools.Field(typeof(Ability), "inCooldown");
                    var cooldownEndTickField = AccessTools.Field(typeof(Ability), "cooldownEndTick");
                    var cooldownDurationField = AccessTools.Field(typeof(Ability), "cooldownDuration");

                    inCooldownField.SetValue(newAbility, true);
                    cooldownEndTickField.SetValue(newAbility, GenTicks.TicksGame + savedState.cooldownTicksRemaining);
                    cooldownDurationField.SetValue(newAbility, savedState.cooldownTicksTotal);
                }

                _savedAbilityStates.Remove(savedState);
            } else if (!isPostLoad) {
                newAbility.RemainingCharges = newAbility.maxCharges;
            }
        }

        if (abilitiesToRemove.Any() || propsToAdd.Any()) {
            _holder.abilities.Notify_TemporaryAbilitiesChanged();
        }

        if (isPostLoad) {
            _savedAbilityStates = null;
        }
    }

    public override void PostExposeData() {
        base.PostExposeData();
        Scribe_References.Look(ref _holder, "holder");

        if (Scribe.mode is LoadSaveMode.Saving) {
            var defNameList = _abilityPropsToManage
                .Select(p => p.abilityDef.defName).ToList();
            Scribe_Collections.Look(ref defNameList, "abilityPropsToManageDefNames", LookMode.Value);
        }

        if (Scribe.mode is LoadSaveMode.LoadingVars) {
            List<string> defNameList = null;
            Scribe_Collections.Look(ref defNameList, "abilityPropsToManageDefNames", LookMode.Value);
            if (defNameList is not null) {
                _abilityPropsToManage = new List<CompProperties_EquippableAbility>();
                foreach (var defName in defNameList) {
                    var trait = DefDatabase<WeaponTraitDef>.AllDefs.FirstOrDefault(t =>
                        t.abilityProps?.abilityDef.defName == defName);
                    if (trait is not null) {
                        _abilityPropsToManage.Add(trait.abilityProps);
                    }
                }
            }
        }

        if (Scribe.mode is LoadSaveMode.Saving) {
            var abilityStates = _managedAbilities.Keys.Select(ability => new AbilityState(ability)).ToList();
            Scribe_Collections.Look(ref abilityStates, "abilityStates", LookMode.Deep);
        }

        if (Scribe.mode is LoadSaveMode.LoadingVars) {
            Scribe_Collections.Look(ref _savedAbilityStates, "abilityStates", LookMode.Deep);
        }
    }
}

public class AbilityState : IExposable {
    public string defName;
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