using RimWorld;
using Verse;

namespace CWF;

public class SpecDatabase {
    internal Spec Range;
    internal Spec BurstShotCount;
    internal Spec WarmupTime;
    internal Spec Cooldown;
    internal Spec Damage;
    internal Spec ArmorPenetration;
    internal Spec StoppingPower;
    internal Spec AccuracyTouch;
    internal Spec AccuracyShort;
    internal Spec AccuracyMedium;
    internal Spec AccuracyLong;
    internal Spec Mass;
    internal Spec Dps;
    private Spec _ticksBetweenBurstShots;

    private readonly Thing _weapon;
    private readonly CompDynamicTraits _compDynamicTraits;

    private enum Mode {
        Raw,
        Dynamic
    }

    public bool IsMeleeWeapon => _weapon.def.IsMeleeWeapon;

    public SpecDatabase(Thing weapon) {
        _weapon = weapon;
        _compDynamicTraits = _weapon.TryGetComp<CompDynamicTraits>();

        // Raw values
        // === Stat ===
        var weaponDef = weapon.def;
        Mass = new Spec(weaponDef.GetStatValueAbstract(StatDefOf.Mass));
        Cooldown = new Spec(weaponDef.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown));
        AccuracyTouch = new Spec(weaponDef.GetStatValueAbstract(StatDefOf.AccuracyTouch));
        AccuracyShort = new Spec(weaponDef.GetStatValueAbstract(StatDefOf.AccuracyShort));
        AccuracyMedium = new Spec(weaponDef.GetStatValueAbstract(StatDefOf.AccuracyMedium));
        AccuracyLong = new Spec(weaponDef.GetStatValueAbstract(StatDefOf.AccuracyLong));

        // === Verb ===
        var weaponDefVerb = weaponDef.Verbs.FirstOrFallback();
        if (weaponDefVerb != null) {
            Range = new Spec(weaponDefVerb.range);
            WarmupTime = new Spec(weaponDefVerb.warmupTime);
            BurstShotCount = new Spec(weaponDefVerb.burstShotCount);
            _ticksBetweenBurstShots = new Spec(weaponDefVerb.ticksBetweenBurstShots);

            // === Projectile ===
            var weaponDefProjectile = weaponDefVerb.defaultProjectile?.projectile;
            if (weaponDefProjectile != null) {
                Damage = new Spec(weaponDefProjectile.GetDamageAmount(weaponDef, weapon.Stuff));
                ArmorPenetration = new Spec(weaponDefProjectile.GetArmorPenetration());
                StoppingPower = new Spec(weaponDefProjectile.stoppingPower);
            }
        }

        Dps = new Spec(CalculateDps(Mode.Raw));

        Recalculate(); // init calc
    }

    public void Recalculate() {
        // === Stat ===
        Mass.Dynamic = _weapon.GetStatValue(StatDefOf.Mass);
        Cooldown.Dynamic = _weapon.GetStatValue(StatDefOf.RangedWeapon_Cooldown);
        AccuracyTouch.Dynamic = _weapon.GetStatValue(StatDefOf.AccuracyTouch);
        AccuracyShort.Dynamic = _weapon.GetStatValue(StatDefOf.AccuracyShort);
        AccuracyMedium.Dynamic = _weapon.GetStatValue(StatDefOf.AccuracyMedium);
        AccuracyLong.Dynamic = _weapon.GetStatValue(StatDefOf.AccuracyLong);

        // === Verb ===
        var weaponVerb = _weapon.TryGetComp<CompEquippableAbilityReloadable>()?.PrimaryVerb
                         ?? _weapon.TryGetComp<CompEquippable>()?.PrimaryVerb;

        Range.Dynamic = Range.Raw * _weapon.GetStatValue(StatDefOf.RangedWeapon_RangeMultiplier);
        WarmupTime.Dynamic = weaponVerb?.verbProps.warmupTime ?? -1;
        BurstShotCount.Dynamic = weaponVerb?.BurstShotCount ?? -1; // harmony patched
        _ticksBetweenBurstShots.Dynamic = weaponVerb?.TicksBetweenBurstShots ?? -1; // harmony patched

        // === Projectile ===
        var weaponDefProjectile = _weapon.def.Verbs.FirstOrFallback()?.defaultProjectile?.projectile;
        if (weaponDefProjectile != null) {
            Damage.Dynamic = weaponDefProjectile.GetDamageAmount(_weapon);
            ArmorPenetration.Dynamic = weaponDefProjectile.GetArmorPenetration(_weapon);
            StoppingPower.Dynamic = GetComputedStoppingPower(); // harmony patched
        }

        Dps.Dynamic = CalculateDps(Mode.Dynamic);

        if (Prefs.DevMode) {
            Log.Message("[CWF Dev]: Recalculated");
        }
    }

    // === Helper ===
    private float CalculateDps(Mode mode) {
        var damage = mode == Mode.Raw ? Damage.Raw : Damage.Dynamic;
        var burstCount = mode == Mode.Raw ? BurstShotCount.Raw : BurstShotCount.Dynamic;
        var ticksBetweenShots = mode == Mode.Raw
            ? _ticksBetweenBurstShots.Raw
            : _ticksBetweenBurstShots.Dynamic;
        var warmup = mode == Mode.Raw ? WarmupTime.Raw : WarmupTime.Dynamic;
        var cooldown = mode == Mode.Raw ? Cooldown.Raw : Cooldown.Dynamic;

        var totalDamage = damage * burstCount;
        var totalBurstSec = ticksBetweenShots * (burstCount - 1) / 60f;
        var totalCycleSec = warmup + cooldown + totalBurstSec;
        return totalCycleSec <= 0 ? 0f : totalDamage / totalCycleSec;
    }

    private float GetComputedStoppingPower() {
        var basePower = _weapon.def.Verbs
            .FirstOrFallback()?.defaultProjectile?.projectile.stoppingPower ?? 0.5f;

        // CompUniqueWeapon
        if (_weapon.TryGetComp<CompUniqueWeapon>(out var compUniqueWeapon)) {
            basePower += compUniqueWeapon.TraitsListForReading.Sum(trait => trait.additionalStoppingPower);
        }

        if (_compDynamicTraits == null) return basePower;

        // CompDynamicTraits
        var additional = _compDynamicTraits.Traits.Sum(traitDef => traitDef.additionalStoppingPower);

        return basePower + additional;
    }
}

internal struct Spec {
    public readonly float Raw;
    public float Dynamic;

    internal Spec(float raw) {
        Raw = raw;
        Dynamic = 0f;
    }
}