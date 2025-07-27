using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CustomizeWeapon;

public class TraitModuleExtension : DefModExtension {
    public WeaponTraitDef weaponTraitDef;
    public Part part;
    public TextureSet texture;
    public List<WeaponSpecificOffset> offsets;
    public List<ThingDef> requiredWeaponDefs;
    public List<string> requiredWeaponTags;
    public List<ThingDef> excludeWeaponDefs;
    public List<string> excludeWeaponTags;

    // public Rarity rarity; // enum
}