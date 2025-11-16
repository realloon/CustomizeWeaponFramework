using Verse;

namespace CWF;

[Obsolete]
public static class PartEnumConverter {
    public static PartDef? Convert(Part oldPart) {
        var partDef = oldPart switch {
            Part.Muzzle => PartDefOf.Muzzle,
            Part.Barrel => PartDefOf.Barrel,
            Part.Receiver => PartDefOf.Receiver,
            Part.Trigger => PartDefOf.Trigger,
            Part.Stock => PartDefOf.Stock,
            Part.Grip => PartDefOf.Grip,
            Part.Sight => PartDefOf.Sight,
            Part.Magazine => PartDefOf.Magazine,
            Part.Underbarrel => PartDefOf.Underbarrel,
            Part.Ammo => PartDefOf.Ammo,
            _ => null
        };

        if (partDef == null) {
            Log.Error($"[CWF] Could not convert old Part enum value '{oldPart}' to a PartDef.");
        }

        return partDef;
    }
}

[Obsolete]
public enum Part {
    None = 0,
    Muzzle,
    Barrel,
    Receiver,
    Trigger,
    Stock,
    Grip,
    Sight,
    Magazine,
    Underbarrel,
    Ammo
}