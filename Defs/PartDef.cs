using JetBrains.Annotations;
using Verse;

// ReSharper disable InconsistentNaming

namespace CWF;

/// <summary>
/// Defines a part slot on a weapon, like a Muzzle or Stock. Used to organize where modules can be attached.
/// </summary>
public class PartDef : Def {
    /// <summary>
    /// Determines the visual layout position of this part slot in the customization UI.
    /// </summary>
    [UsedImplicitly]
    public readonly PartGroup group;

    /// <summary>
    /// The sorting order of this part within its UI group.
    /// </summary>
    [UsedImplicitly]
    public readonly int order;

    public override IEnumerable<string> ConfigErrors() {
        foreach (var item in base.ConfigErrors()) {
            yield return item;
        }

        if (group == PartGroup.None) {
            yield return "group not set";
        }
    }
}

/// <summary>
/// An enumeration that defines the layout group for a `PartDef` in the weapon customization UI (e.g., Top, Bottom, Left, Right). This determines where the attachment slot will be visually displayed.
/// </summary>
public enum PartGroup {
    /// <summary>
    /// Default value, should not be used.
    /// </summary>
    None = 0,

    /// <summary>
    /// The top row of the UI.
    /// </summary>
    Top,

    /// <summary>
    /// The bottom row of the UI.
    /// </summary>
    Bottom,

    /// <summary>
    /// The left column of the UI.
    /// </summary>
    Left,

    /// <summary>
    /// The right column of the UI.
    /// </summary>
    Right
}