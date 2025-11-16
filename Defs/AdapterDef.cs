using JetBrains.Annotations;
using Verse;

// ReSharper disable InconsistentNaming

namespace CWF;

/// <summary>
/// A special Def used to adapt existing vanilla or modded weapons to the CWF system without direct patching. An AdapterDef targets a weapon by its defName and injects necessary components (`comps`), tags (`weaponTags`), and a new base graphic (`graphicData`), making it fully compatible with the framework.
/// </summary>
public class AdapterDef : Def {
    /// <summary>
    /// The new base graphic data to apply to the adapted weapon.
    /// </summary>
    [UsedImplicitly]
    public readonly GraphicData? graphicData;

    /// <summary>
    /// Additional weaponTags to add to the adapted weapon.
    /// </summary>
    [UsedImplicitly]
    public readonly List<string> weaponTags = [];

    /// <summary>
    /// A list of CompProperties to inject into the adapted weapon, such as `CompProperties_DynamicTraits`.
    /// </summary>
    [UsedImplicitly]
    public readonly List<CompProperties> comps = [];

    public override IEnumerable<string> ConfigErrors() {
        foreach (var item in base.ConfigErrors()) {
            yield return item;
        }

        if (graphicData == null) {
            yield return "graphicData is null";
        }
    }

    internal static void Inject() {
        var allAdapters = DefDatabase<AdapterDef>.AllDefs;

        foreach (var adapter in allAdapters) {
            var weaponDef = DefDatabase<ThingDef>.GetNamed(adapter.defName, false);

            if (weaponDef == null) {
                Log.Warning($"[CWF] AdapterDef '{adapter.defName}' could not find a matching ThingDef to adapt.");
                continue;
            }

            AdaptWeapon(weaponDef, adapter);
        }
    }

    #region Helper

    private static void AdaptWeapon(ThingDef weaponDef, AdapterDef adapter) {
        // === graphicData ===
        weaponDef.graphicData.texPath = adapter.graphicData?.texPath;
        weaponDef.graphicData.graphicClass = adapter.graphicData?.graphicClass;
        weaponDef.graphicData.shaderType = adapter.graphicData?.shaderType;

        // === weaponTags ===
        if (adapter.weaponTags.Count > 0) {
            weaponDef.weaponTags ??= [];
            weaponDef.weaponTags = weaponDef.weaponTags.Union(adapter.weaponTags).ToList();
        }

        // === comps ===
        weaponDef.comps ??= [];

        // injector default comps
        TryAddComp(weaponDef, new CompProperties_Renamable());
        TryAddComp(weaponDef, new CompProperties_Colorable());
        TryAddComp(weaponDef, new CompProperties_AbilityProvider());

        // injector adapter inner comps
        foreach (var compToAdd in adapter.comps) {
            TryAddComp(weaponDef, compToAdd);
        }
    }

    private static void TryAddComp(ThingDef weaponDef, CompProperties newComp) {
        if (!weaponDef.comps.Any(comp => comp.compClass == newComp.compClass)) {
            weaponDef.comps.Add(newComp);
        }
    }

    #endregion
}