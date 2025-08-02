using RimWorld;
using Verse;

namespace CustomizeWeapon;

public class CompTraitModule : ThingComp {
    // private CompProperties_TraitModule Props => (CompProperties_TraitModule)props;

    private WeaponTraitDef _cachedTraitDef;
    private bool _isTraitDefCached;

    private WeaponTraitDef TraitDef {
        get {
            if (_isTraitDefCached) return _cachedTraitDef;

            _cachedTraitDef = parent.def.GetModExtension<TraitModuleExtension>()?.weaponTraitDef;
            _isTraitDefCached = true;
            return _cachedTraitDef;
        }
    }

    public override string TransformLabel(string label) {
        return TraitDef?.LabelCap ?? base.TransformLabel(label);
    }

    public override string GetDescriptionPart() {
        return TraitDef?.description;
    }
}