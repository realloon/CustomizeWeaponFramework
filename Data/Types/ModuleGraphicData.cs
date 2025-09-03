using UnityEngine;
using Verse;

// ReSharper disable UnassignedField.Global

namespace CWF;

// ReSharper disable once ClassNeverInstantiated.Global
public class ModuleGraphicData {
    [NoTranslate] public string? texturePath;
    [NoTranslate] public string? outlinePath;
    public Vector2? offset;
    public float? scale;
}