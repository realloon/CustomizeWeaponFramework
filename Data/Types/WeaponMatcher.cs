using Verse;
using CWF.Extensions;

// ReSharper disable UnassignedField.Global

namespace CWF;

// ReSharper disable once ClassNeverInstantiated.Global
public class WeaponMatcher {
    public List<ThingDef>? weaponDefs;
    public List<string>? weaponTags;

    public bool IsMatch(ThingDef weaponDef) {
        if (!weaponDefs.IsNullOrEmpty() && weaponDefs.Contains(weaponDef)) return true;

        if (weaponTags.IsNullOrEmpty() || weaponDef.weaponTags.NullOrEmpty()) return false;

        return weaponTags.Any(tag => weaponDef.weaponTags.Contains(tag));
    }
}