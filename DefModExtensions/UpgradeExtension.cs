using Verse;

namespace CWF;

// ReSharper disable once ClassNeverInstantiated.Global
public class UpgradeExtension : DefModExtension {
    public readonly ThingDef baseWeaponDef = new();

    public override IEnumerable<string> ConfigErrors() {
        if (baseWeaponDef.defName == "UnnamedDef") {
            yield return "Required field 'baseWeaponDef' is missing in XML.";
        }
    }
}