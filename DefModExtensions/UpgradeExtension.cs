using JetBrains.Annotations;
using Verse;

namespace CWF;

[UsedImplicitly]
public class UpgradeExtension : DefModExtension {
    [UsedImplicitly]
    public readonly ThingDef? baseWeaponDef;

    public override IEnumerable<string> ConfigErrors() {
        if (baseWeaponDef == null) {
            yield return "Required field 'baseWeaponDef' is missing in XML.";
        }
    }
}