using UnityEngine;
using Verse;

namespace CustomizeWeapon.ViewDrawers;

public class AsideDrawer {
    private readonly SpecDatabase _specDatabase;

    public AsideDrawer(SpecDatabase specDatabase) {
        _specDatabase = specDatabase;
    }

    public void Draw(Rect rect) {
        var listing = new Listing_Standard();
        listing.Begin(rect);

        if (_specDatabase.IsMeleeWeapon) {
            listing.Label("CWF_UI_NotRanged".Translate());
            listing.End();
            return;
        }

        // === Weapon Stats ===
        DrawStatRow(listing, "CWF_UI_Range".Translate(), _specDatabase.Range);
        DrawStatRow(listing, "CWF_UI_RangeDPS".Translate(), _specDatabase.Dps);
        DrawStatRow(listing, "CWF_UI_RangeBurstCount".Translate(), _specDatabase.BurstShotCount);
        DrawStatRow(listing, "CWF_UI_RangeWarmupTime".Translate(), _specDatabase.WarmupTime, "F1", " s", true);
        DrawStatRow(listing, "CWF_UI_RangeCooldown".Translate(), _specDatabase.Cooldown, "F1", " s", true);

        // === Projectile Stats ===
        listing.GapLine();
        listing.Label($"<color=#999999><b>{"CWF_UI_Projectile".Translate()}</b></color>", 22f);
        DrawStatRow(listing, "CWF_UI_Damage".Translate(), _specDatabase.Damage);
        DrawStatRow(listing, "CWF_UI_ArmorPenetration".Translate(), _specDatabase.ArmorPenetration, "N0", "%");
        DrawStatRow(listing, "CWF_UI_StoppingPower".Translate(), _specDatabase.StoppingPower, "F1");

        // === Accuracy Stats ===
        listing.GapLine();
        listing.Label($"<color=#999999><b>{"CWF_UI_Accuracy".Translate()}</b></color>", 22f);
        DrawStatRow(listing, "CWF_UI_Touch".Translate(), _specDatabase.AccuracyTouch, "N0", "%");
        DrawStatRow(listing, "CWF_UI_Short".Translate(), _specDatabase.AccuracyShort, "N0", "%");
        DrawStatRow(listing, "CWF_UI_Medium".Translate(), _specDatabase.AccuracyMedium, "N0", "%");
        DrawStatRow(listing, "CWF_UI_Long".Translate(), _specDatabase.AccuracyLong, "N0", "%");

        // === Other Stats ===
        listing.GapLine();
        DrawStatRow(listing, "CWF_UI_Mass".Translate(), _specDatabase.Mass, "F1", "Kg", true);

        listing.End();
    }

    // helper
    private static void DrawStatRow(
        Listing_Standard listing, string label, Spec spec,
        string format = "N0", string unit = "", bool invertDeltaColor = false) {
        var value = unit == "%" ? spec.Dynamic * 100 : spec.Dynamic;
        var valueString = value.ToString(format) + unit;
        var delta = invertDeltaColor ? spec.Raw - spec.Dynamic : spec.Dynamic - spec.Raw;

        DrawLabelRow(listing.GetRect(22), label, valueString, delta);
    }

    private static void DrawLabelRow(Rect rect, string label, string value, float deltaValue = 0f) {
        var originalAnchor = Text.Anchor;
        var originalColor = GUI.color;

        // Label
        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = Color.white;
        Widgets.Label(rect, label);

        // Value with color
        Text.Anchor = TextAnchor.MiddleRight;

        GUI.color = deltaValue switch {
            > 0f => new Color(0.15f, 0.85f, 0.15f),
            < 0f => new Color(0.87f, 0.49f, 0.51f),
            _ => Color.white
        };

        Widgets.Label(rect, value);

        // Restore
        Text.Anchor = originalAnchor;
        GUI.color = originalColor;
    }
}