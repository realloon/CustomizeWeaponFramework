using UnityEngine;
using Verse;

namespace CWF.ViewDrawers;

public class AsideDrawer(SpecDatabase specDatabase) {
    public void Draw(in Rect rect) {
        var listing = new Listing_Standard();
        listing.Begin(rect);

        if (specDatabase.IsMeleeWeapon) {
            listing.Label("CWF_UI_NotRanged".Translate());
            listing.End();
            return;
        }

        // === Weapon Stats ===
        DrawStatRow(listing, "CWF_UI_Range".Translate(), specDatabase.Range);
        DrawStatRow(listing, "CWF_UI_RangeDPS".Translate(), specDatabase.Dps);
        DrawStatRow(listing, "CWF_UI_RangeBurstCount".Translate(), specDatabase.BurstShotCount);
        DrawStatRow(listing, "CWF_UI_RangeWarmupTime".Translate(), specDatabase.WarmupTime, "F1", " s");
        DrawStatRow(listing, "CWF_UI_RangeCooldown".Translate(), specDatabase.Cooldown, "F1", " s");

        // === Projectile Stats ===
        listing.GapLine();
        listing.Label($"<color=#999999><b>{"CWF_UI_Projectile".Translate()}</b></color>", 22f);
        DrawStatRow(listing, "CWF_UI_Damage".Translate(), specDatabase.Damage);
        DrawStatRow(listing, "CWF_UI_ArmorPenetration".Translate(), specDatabase.ArmorPenetration, unit: "%");
        DrawStatRow(listing, "CWF_UI_StoppingPower".Translate(), specDatabase.StoppingPower, "F1");

        // === Accuracy Stats ===
        listing.GapLine();
        listing.Label($"<color=#999999><b>{"CWF_UI_Accuracy".Translate()}</b></color>", 22f);
        DrawStatRow(listing, "CWF_UI_Touch".Translate(), specDatabase.AccuracyTouch, unit: "%");
        DrawStatRow(listing, "CWF_UI_Short".Translate(), specDatabase.AccuracyShort, unit: "%");
        DrawStatRow(listing, "CWF_UI_Medium".Translate(), specDatabase.AccuracyMedium, unit: "%");
        DrawStatRow(listing, "CWF_UI_Long".Translate(), specDatabase.AccuracyLong, unit: "%");

        // === Other Stats ===
        listing.GapLine();
        DrawStatRow(listing, "Mass".Translate(), specDatabase.Mass, "F1", "Kg");
        DrawStatRow(listing, "MarketValueTip".Translate(), specDatabase.MarketValue);

        listing.End();
    }

    // helper
    private static void DrawStatRow(
        Listing_Standard listing, string label, Spec spec,
        string format = "N0", string unit = "") {
        var value = unit == "%" ? spec.Dynamic * 100 : spec.Dynamic;
        var valueString = value.ToString(format) + unit;
        var delta = spec.IsLowerValueBetter ? spec.Raw - spec.Dynamic : spec.Dynamic - spec.Raw;

        DrawLabelRow(listing.GetRect(22), label, valueString, delta);
    }

    private static void DrawLabelRow(in Rect rect, string label, string value, float deltaValue = 0f) {
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