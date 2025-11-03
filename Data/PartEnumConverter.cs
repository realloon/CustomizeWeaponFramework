using Verse;

namespace CWF;

public static class PartEnumConverter {
    public static PartDef? Convert(Part oldPart) {
        var defName = oldPart switch {
            Part.Muzzle => "Muzzle",
            Part.Barrel => "Barrel",
            Part.Receiver => "Receiver",
            Part.Trigger => "Trigger",
            Part.Stock => "Stock",
            Part.Grip => "Grip",
            Part.Sight => "Sight",
            Part.Magazine => "Magazine",
            Part.Underbarrel => "Underbarrel",
            Part.Ammo => "Ammo",
            _ => null
        };

        if (defName != null) return DefDatabase<PartDef>.GetNamed(defName, false);

        Log.Error($"[CWF] Could not convert old Part enum value '{oldPart}' to a PartDef.");
        return null;
    }
}

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