using UnityEngine;
using Verse;

namespace CustomizeWeapon;

public class ModuleGraphicData {
    [NoTranslate] public string texturePath;
    [NoTranslate] public string outlinePath;
    public Vector2? offset;
    public float? scale;
}