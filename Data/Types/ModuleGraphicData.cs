using JetBrains.Annotations;
using UnityEngine;
using Verse;

// ReSharper disable InconsistentNaming

namespace CWF;

/// <summary>
/// A data container class that holds all the information needed to render a single module's graphic, including its `texturePath`, `outlinePath`, `offset`, and `scale`.
/// </summary>
[UsedImplicitly]
public class ModuleGraphicData {
    /// <summary>
    /// The file path to the module's main texture.
    /// </summary>
    [NoTranslate]
    [UsedImplicitly]
    public string? texturePath;

    /// <summary>
    /// Optional file path to an outline texture, drawn behind the main texture.
    /// </summary>
    [NoTranslate]
    [UsedImplicitly]
    public string? outlinePath;

    /// <summary>
    /// The X/Y pixel offset to apply when rendering the graphic.
    /// </summary>
    [UsedImplicitly]
    public Vector2? offset;

    /// <summary>
    /// A multiplier for the size of the graphic. Defaults to 1.0.
    /// </summary>
    [UsedImplicitly]
    public float? scale;
}