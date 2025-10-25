using JetBrains.Annotations;

namespace CWF;

[UsedImplicitly]
public class ConditionalPartModifier {
    [UsedImplicitly]
    public readonly WeaponMatcher? matcher;

    [UsedImplicitly]
    public readonly List<Part> enablesParts = [];

    [UsedImplicitly]
    public readonly List<Part> disablesParts = [];
}