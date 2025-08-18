using RimWorld;
using UnityEngine;
using Verse;

namespace CWF.ViewDrawers;

public class MainDrawer {
    // Data source
    private readonly Thing _weapon;
    private readonly CompDynamicTraits _compDynamicTraits;
    private readonly HashSet<Part> _supportParts = new();
    private readonly Action<Part, WeaponTraitDef> _onSlotClick;

    private const float SlotSize = 56f;
    private const float SlotPadding = 12f;

    private enum Direction {
        Horizontal,
        Vertical
    }

    private static readonly Part[] TopParts = { Part.Receiver, Part.Sight, Part.Barrel };
    private static readonly Part[] LeftParts = { Part.Stock };
    private static readonly Part[] RightParts = { Part.Muzzle, Part.Ammo };
    private static readonly Part[] BottomParts = { Part.Grip, Part.Trigger, Part.Magazine, Part.Underbarrel };

    public MainDrawer(Thing weapon, Action<Part, WeaponTraitDef> onSlotClick) {
        _weapon = weapon;
        _compDynamicTraits = weapon.TryGetComp<CompDynamicTraits>();

        if (_compDynamicTraits?.props is CompProperties_DynamicTraits props && !props.supportParts.NullOrEmpty()) {
            _supportParts = new HashSet<Part>(props.supportParts);
        }

        _onSlotClick = onSlotClick;
    }

    public void Draw(Rect rect) {
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
        var weaponGraphic = _weapon.Graphic;
        var weaponAspect = weaponGraphic.drawSize.x / weaponGraphic.drawSize.y;
        var iconWidth = middleCenterRect.width;
        var iconHeight = middleCenterRect.height;
        iconWidth = iconWidth / weaponAspect > iconHeight ? iconHeight * weaponAspect : iconWidth / weaponAspect;

        // render weapon icon
        var weaponIconRect = new Rect(middleCenterRect.center.x - iconWidth / 2f,
            middleCenterRect.center.y - iconHeight / 2f, iconWidth, iconHeight);
        Widgets.ThingIcon(weaponIconRect, _weapon);

        DrawPartGroup(topCenterRect, TopParts, Direction.Horizontal);
        DrawPartGroup(middleLeftRect, LeftParts, Direction.Horizontal);
        DrawPartGroup(middleRightRect, RightParts, Direction.Vertical);
        DrawPartGroup(bottomCenterRect, BottomParts, Direction.Horizontal);
    }

    // === Helper ===
    private void DrawPartGroup(Rect container, IReadOnlyList<Part> groupParts, Direction direction) {
        var supportedParts = groupParts.Where(p => _supportParts.Contains(p)).ToList();
        if (supportedParts.Count == 0) return;

        var count = supportedParts.Count;
        if (direction == Direction.Horizontal) {
            var totalWidth = (count * SlotSize) + (Math.Max(0, count - 1) * SlotPadding);
            var startX = container.center.x - totalWidth / 2f;
            var startY = container.center.y - SlotSize / 2f;

            for (var i = 0; i < count; i++) {
                var part = supportedParts[i];
                var slotRect = new Rect(startX + i * (SlotSize + SlotPadding), startY, SlotSize, SlotSize);
                TryDrawSlot(part, slotRect);
            }
        } else {
            // Vertical
            var totalHeight = (count * SlotSize) + (Math.Max(0, count - 1) * SlotPadding);
            var startX = container.center.x - SlotSize / 2f;
            var startY = container.center.y - totalHeight / 2f;

            for (var i = 0; i < count; i++) {
                var part = supportedParts[i];
                var slotRect = new Rect(startX, startY + i * (SlotSize + SlotPadding), SlotSize, SlotSize);
                TryDrawSlot(part, slotRect);
            }
        }
    }

    private void TryDrawSlot(Part part, Rect rect) {
        if (!_supportParts.Contains(part)) return;

        var installedTrait = _compDynamicTraits?.GetInstalledTraitFor(part);
        bool clicked;

        if (installedTrait != null) {
            Widgets.DrawOptionBackground(rect, Mouse.IsOver(rect));

            var compDynamicGraphic = _weapon.TryGetComp<CompDynamicGraphic>();
            var moduleGraphicData = compDynamicGraphic.GetGraphicDataFor(installedTrait);

            if (moduleGraphicData != null && !string.IsNullOrEmpty(moduleGraphicData.texturePath)) {
                DrawModuleTexture(rect, moduleGraphicData);
            } else {
                var originalAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, installedTrait.LabelCap);
                Text.Anchor = originalAnchor;
            }

            TooltipHandler.TipRegion(rect, $"{installedTrait.LabelCap}\n{installedTrait.description ?? ""}\n\n" +
                                           string.Join("\n", CompTraitModule.GetTraitEffectLines(installedTrait)));

            clicked = Widgets.ButtonInvisible(rect);
        } else {
            clicked = DrawPartSlot(rect, $"CWF_UI_{part.ToString()}".Translate());
        }

        if (clicked) _onSlotClick?.Invoke(part, installedTrait);
    }

    private static void DrawModuleTexture(Rect rect, ModuleGraphicData moduleGraphicData) {
        // Outline
        if (!string.IsNullOrEmpty(moduleGraphicData.outlinePath)) {
            var outlineTexture = ContentFinder<Texture2D>.Get(moduleGraphicData.outlinePath, false);
            if (outlineTexture != null) {
                Widgets.DrawTextureFitted(rect, outlineTexture, 1f);
            }
        }

        // module
        var mainTexture = ContentFinder<Texture2D>.Get(moduleGraphicData.texturePath);
        if (mainTexture != null) {
            Widgets.DrawTextureFitted(rect, mainTexture, 1f);
        }
    }

    // Returns a boolean indicating whether the element was clicked.
    private static bool DrawPartSlot(Rect rect, string label) {
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