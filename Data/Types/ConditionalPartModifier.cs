using JetBrains.Annotations;

namespace CWF;

[UsedImplicitly]
public class ConditionalPartModifier {
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public readonly WeaponMatcher? matcher;

    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public readonly List<Part> enablesParts = [];

    [UsedImplicitly]
    public readonly List<Part> disablesParts = [];
}