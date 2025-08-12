using Verse;

namespace CWF;

public class CompProperties_DynamicGraphic : CompProperties {
    public List<AttachmentPointData> attachmentPoints = new();

    public CompProperties_DynamicGraphic() => compClass = typeof(CompDynamicGraphic);
}