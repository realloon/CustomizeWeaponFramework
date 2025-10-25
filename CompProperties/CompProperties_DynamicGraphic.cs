using JetBrains.Annotations;
using Verse;

namespace CWF;

[UsedImplicitly]
public class CompProperties_DynamicGraphic : CompProperties {
    [UsedImplicitly]
    public readonly List<AttachmentPointData> attachmentPoints = [];

    public CompProperties_DynamicGraphic() => compClass = typeof(CompDynamicGraphic);
}