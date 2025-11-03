using RimWorld;

namespace CWF;

[DefOf]
public class PartDefOf {
    public static readonly PartDef Receiver = null!;
    public static readonly PartDef Sight = null!;
    public static readonly PartDef Barrel = null!;
    public static readonly PartDef Stock = null!;
    public static readonly PartDef Muzzle = null!;
    public static readonly PartDef Ammo = null!;
    public static readonly PartDef Grip = null!;
    public static readonly PartDef Trigger = null!;
    public static readonly PartDef Magazine = null!;
    public static readonly PartDef Underbarrel = null!;

    static PartDefOf() {
        DefOfHelper.EnsureInitializedInCtor(typeof(PartDefOf));
    }
}