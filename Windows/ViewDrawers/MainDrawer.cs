using RimWorld;
using UnityEngine;
using Verse;

namespace CWF.ViewDrawers;

public class MainDrawer(Thing weapon, Action<Part, WeaponTraitDef?> onSlotClick) {
    // Data source
    private readonly CompDynamicTraits? _compDynamicTraits = weapon.TryGetComp<CompDynamicTraits>();

    private const float SlotSize = 56f;
    private const float SlotPadding = 12f;

    private enum Direction {
        Horizontal,
        Vertical
    }

    private static readonly Part[] TopParts = [Part.Receiver, Part.Sight, Part.Barrel];
    private static readonly Part[] LeftParts = [Part.Stock];
    private static readonly Part[] RightParts = [Part.Muzzle, Part.Ammo];
    private static readonly Part[] BottomParts = [Part.Grip, Part.Trigger, Part.Magazine, Part.Underbarrel];

    public void Draw(in Rect rect) {
        // define gird row Height
        const float topRowHeight = SlotSize + SlotPadding;
        const float bottomRowHeight = SlotSize + SlotPadding;
        var middleRowHeight = rect.height - topRowHeight - bottomRowHeight;

        // define gird col with
        const float leftColWidth = SlotSize + SlotPadding;
        const float rightColWidth = SlotSize + SlotPadding;
        var middleColWidth = rect.width - leftColWidth - rightColWidth;

        if (middleRowHeight <= 0 || middleColWidth <= 0) return;

        // define gird cell
        var topCenterRect = new Rect(rect.x + leftColWidth, rect.y, middleColWidth, topRowHeight);
        var middleLeftRect = new Rect(rect.x, rect.y + topRowHeight, leftColWidth, middleRowHeight);
        var middleCenterRect =
            new Rect(rect.x + leftColWidth, rect.y + topRowHeight, middleColWidth, middleRowHeight);
        var middleRightRect = new Rect(rect.x + leftColWidth + middleColWidth, rect.y + topRowHeight, rightColWidth,
            middleRowHeight);
        var bottomCenterRect = new Rect(rect.x + leftColWidth, rect.y + topRowHeight + middleRowHeight,
            middleColWidth, bottomRowHeight);

        // define weapon icon
        var weaponGraphic = weapon.Graphic;
        var weaponAspect = weaponGraphic.drawSize.x / weaponGraphic.drawSize.y;
        var iconWidth = middleCenterRect.width;
        var iconHeight = middleCenterRect.height;
        iconWidth = iconWidth / weaponAspect > iconHeight ? iconHeight * weaponAspect : iconWidth / weaponAspect;

        // render weapon icon
        var weaponIconRect = new Rect(middleCenterRect.center.x - iconWidth / 2f,
            middleCenterRect.center.y - iconHeight / 2f, iconWidth, iconHeight);
        Widgets.ThingIcon(weaponIconRect, weapon);

        DrawPartGroup(in topCenterRect, TopParts, Direction.Horizontal);
        DrawPartGroup(in middleLeftRect, LeftParts, Direction.Horizontal);
        DrawPartGroup(in middleRightRect, RightParts, Direction.Vertical);
        DrawPartGroup(in bottomCenterRect, BottomParts, Direction.Horizontal);
    }

    // === Helper ===
    private void DrawPartGroup(in Rect container, IReadOnlyList<Part> groupParts, Direction direction) {
        if (_compDynamicTraits == null) return;

        var supportedParts = groupParts.Where(p => _compDynamicTraits.AvailableParts.Contains(p)).ToList();
        if (supportedParts.Count == 0) return;

        var count = supportedParts.Count;
        if (direction == Direction.Horizontal) {
            var totalWidth = (count * SlotSize) + (Math.Max(0, count - 1) * SlotPadding);
            var startX = container.center.x - totalWidth / 2f;
            var startY = container.center.y - SlotSize / 2f;

            for (var i = 0; i < count; i++) {
                var part = supportedParts[i];
                var slotRect = new Rect(startX + i * (SlotSize + SlotPadding), startY, SlotSize, SlotSize);
                TryDrawSlot(part, in slotRect);
            }
        } else {
            // Vertical
            var totalHeight = (count * SlotSize) + (Math.Max(0, count - 1) * SlotPadding);
            var startX = container.center.x - SlotSize / 2f;
            var startY = container.center.y - totalHeight / 2f;

            for (var i = 0; i < count; i++) {
                var part = supportedParts[i];
                var slotRect = new Rect(startX, startY + i * (SlotSize + SlotPadding), SlotSize, SlotSize);
                TryDrawSlot(part, in slotRect);
            }
        }
    }

    private void TryDrawSlot(Part part, in Rect rect) {
        if (_compDynamicTraits == null || !_compDynamicTraits.AvailableParts.Contains(part)) return;

        var installedTrait = _compDynamicTraits.GetInstalledTraitFor(part);
        bool clicked;

        if (installedTrait != null) {
            Widgets.DrawOptionBackground(rect, Mouse.IsOver(rect));

            var moduleGraphicData = weapon.TryGetComp<CompDynamicGraphic>()?.GetGraphicDataFor(installedTrait);

            if (moduleGraphicData != null && !string.IsNullOrEmpty(moduleGraphicData.texturePath)) {
                DrawModuleTexture(in rect, moduleGraphicData);
            } else {
                var originalFont = Text.Font;
                var originalAnchor = Text.Anchor;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, installedTrait.LabelCap);
                Text.Font = originalFont;
                Text.Anchor = originalAnchor;
            }

            var tip =
                $"<b>{installedTrait.LabelCap}</b>\n" +
                (!installedTrait.description.NullOrEmpty() ? $"{installedTrait.description}\n\n" : string.Empty) +
                TraitModuleDatabase.GetTraitEffectLines(installedTrait).ToLineList();

            TooltipHandler.TipRegion(rect, tip);

            clicked = Widgets.ButtonInvisible(rect);
        } else {
            clicked = DrawPartSlot(in rect, $"CWF_UI_{part.ToString()}".Translate());
        }

        if (clicked) onSlotClick.Invoke(part, installedTrait);
    }

    private static void DrawModuleTexture(in Rect rect, ModuleGraphicData moduleGraphicData) {
        // Outline
        var outlineTexture = CompDynamicGraphic.GetOutlineTexture(moduleGraphicData);
        if (outlineTexture != null) {
            Widgets.DrawTextureFitted(rect, outlineTexture, 1f);
        }

        // module
        var mainTexture = ContentFinder<Texture2D>.Get(moduleGraphicData.texturePath);
        if (mainTexture != null) {
            Widgets.DrawTextureFitted(rect, mainTexture, 1f);
        }
    }

    /// Returns a boolean indicating whether the element was clicked.
    private static bool DrawPartSlot(in Rect rect, string label) {
        Widgets.DrawOptionBackground(rect, Mouse.IsOver(rect));

        var originalAnchor = Text.Anchor;
        var originalFont = Text.Font;
        var originalColor = GUI.color;

        // render label
        var labelRect = new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, rect.height - 3f);
        Text.Font = GameFont.Tiny;
        GUI.color = Color.gray;
        Text.Anchor = TextAnchor.LowerLeft;
        Widgets.Label(labelRect, label);

        // render '+'
        Text.Font = GameFont.Medium;
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, "+");

        Text.Anchor = originalAnchor;
        Text.Font = originalFont;
        GUI.color = originalColor;

        return Widgets.ButtonInvisible(rect);
    }
}