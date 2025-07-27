using UnityEngine;
using Verse;

namespace CustomizeWeapon;

public class TextureSet {
    [NoTranslate] public string texturePath;
    [NoTranslate] public string outlinePath;
    public Vector2 offset = Vector2.zero; // new Vector2(0.0f, 0.0f)
}