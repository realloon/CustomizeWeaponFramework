using Verse;

namespace CWF;

// ReSharper disable once ClassNeverInstantiated.Global
public class CompProperties_DynamicGraphic : CompProperties {
    // ReSharper disable once CollectionNeverUpdated.Global
    public readonly List<AttachmentPointData> attachmentPoints = [];

    public CompProperties_DynamicGraphic() => compClass = typeof(CompDynamicGraphic);
}