using JetBrains.Annotations;
using Verse;

namespace CWF;

[UsedImplicitly]
[Obsolete("This class is deprecated and will be removed in a future version. Use AdapterDef system instead.")]
public class UpgradeExtension : DefModExtension {
    [UsedImplicitly]
    public readonly ThingDef? baseWeaponDef;

    public override IEnumerable<string> ConfigErrors() {
        if (baseWeaponDef == null) {
            yield return "Required field 'baseWeaponDef' is missing in XML.";
        }
    }
}