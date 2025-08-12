using Verse;

namespace CWF;

public class WeaponMatcher {
    public List<ThingDef> weaponDefs;
    public List<string> weaponTags;

    public bool IsMatch(ThingDef weaponDef) {
        if (weaponDef == null) return false;

        if (!weaponDefs.NullOrEmpty() && weaponDefs.Contains(weaponDef)) return true;

        if (weaponTags.NullOrEmpty() || weaponDef.weaponTags.NullOrEmpty()) return false;

        return Enumerable.Any(weaponTags, tag => weaponDef.weaponTags.Contains(tag));
    }
}