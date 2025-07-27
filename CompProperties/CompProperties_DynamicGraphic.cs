using System.Collections.Generic;
using Verse;

namespace CustomizeWeapon;

public class CompProperties_DynamicGraphic : CompProperties {
    public List<AttachmentPointData> attachmentPoints = new();

    public CompProperties_DynamicGraphic() => compClass = typeof(CompDynamicGraphic);
}