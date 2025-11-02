using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace CWF;

[UsedImplicitly]
public class ModuleGraphicData {
    [NoTranslate]
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public string? texturePath;

    [NoTranslate]
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public string? outlinePath;

    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public Vector2? offset;

    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public float? scale;
}