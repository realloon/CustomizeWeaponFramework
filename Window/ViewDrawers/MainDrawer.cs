using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CustomizeWeapon.ViewDrawers;

public class MainDrawer {
    // Data source
    private readonly Thing _weapon;
    private readonly CompDynamicTraits _compDynamicTraits;
    private readonly HashSet<Part> _supportParts = new();
    private readonly Action<Part, WeaponTraitDef> _onSlotClick;
    // private readonly CompDynamicGraphic _compDynamicGraphic;

    public MainDrawer(Thing weapon, Action<Part, WeaponTraitDef> onSlotClick) {
        _weapon = weapon;
        _compDynamicTraits = weapon.TryGetComp<CompDynamicTraits>();
        // _compDynamicGraphic = weapon.TryGetComp<CompDynamicGraphic>();

        if (_compDynamicTraits?.props is CompProperties_DynamicTraits props && !props.supportParts.NullOrEmpty()) {
            _supportParts = new HashSet<Part>(props.supportParts);
        }

        _onSlotClick = onSlotClick;
    }

    public void Draw(Rect rect) {
        const float slotSize = 56f;
        const float slotPadding = 12f;

        // define gird row Height
        const float topRowHeight = slotSize + slotPadding;
        const float bottomRowHeight = slotSize + slotPadding;
        var middleRowHeight = rect.height - topRowHeight - bottomRowHeight;

        // define gird col with
        const float leftColWidth = slotSize + slotPadding;
        const float rightColWidth = slotSize + slotPadding;
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
        if (iconWidth / weaponAspect > iconHeight) {
            iconWidth = iconHeight * weaponAspect;
        } else {
            iconHeight = iconWidth / weaponAspect;
        }

        // render weapon icon
        var weaponIconRect = new Rect(middleCenterRect.center.x - iconWidth / 2f,
            middleCenterRect.center.y - iconHeight / 2f, iconWidth, iconHeight);
        Widgets.ThingIcon(weaponIconRect, _weapon);

        // top rects
        const float topSlotTotalWidth = slotSize * 3 + slotPadding * 2;
        var topStartX = topCenterRect.center.x - topSlotTotalWidth / 2f;
        var receiverRect = new Rect(topStartX, topCenterRect.center.y - slotSize / 2f, slotSize, slotSize);
        var sightRect = new Rect(topStartX + slotSize + slotPadding, topCenterRect.center.y - slotSize / 2f,
            slotSize,
            slotSize);
        var barrelRect = new Rect(topStartX + (slotSize + slotPadding) * 2, topCenterRect.center.y - slotSize / 2f,
            slotSize, slotSize);
        // left rect
        var stockRect = new Rect(middleLeftRect.center.x - slotSize / 2f, middleLeftRect.center.y - slotSize / 2f,
            slotSize, slotSize);
        // right rects
        var muzzleRect = new Rect(middleRightRect.center.x - slotSize / 2f,
            middleRightRect.center.y - slotSize / 2f - (slotSize / 2f + slotPadding / 2f), slotSize, slotSize);
        var ammoRect = new Rect(middleRightRect.center.x - slotSize / 2f,
            middleRightRect.center.y - slotSize / 2f + (slotSize / 2f + slotPadding / 2f), slotSize, slotSize);
        // bottom rects
        const float bottomSlotTotalWidth = slotSize * 4 + slotPadding * 3;
        var bottomStartX = bottomCenterRect.center.x - bottomSlotTotalWidth / 2f;
        var gripRect = new Rect(bottomStartX, bottomCenterRect.center.y - slotSize / 2f, slotSize, slotSize);
        var triggerRect = new Rect(bottomStartX + slotSize + slotPadding, bottomCenterRect.center.y - slotSize / 2f,
            slotSize, slotSize);
        var magazineRect = new Rect(bottomStartX + (slotSize + slotPadding) * 2,
            bottomCenterRect.center.y - slotSize / 2f, slotSize, slotSize);
        var underbarrelRect = new Rect(bottomStartX + (slotSize + slotPadding) * 3,
            bottomCenterRect.center.y - slotSize / 2f, slotSize, slotSize);

        // top
        TryDrawSlot(Part.Receiver, receiverRect);
        TryDrawSlot(Part.Sight, sightRect);
        TryDrawSlot(Part.Barrel, barrelRect);
        // left
        TryDrawSlot(Part.Stock, stockRect);
        // right
        TryDrawSlot(Part.Muzzle, muzzleRect);
        TryDrawSlot(Part.Ammo, ammoRect);
        // bottom
        TryDrawSlot(Part.Grip, gripRect);
        TryDrawSlot(Part.Trigger, triggerRect);
        TryDrawSlot(Part.Magazine, magazineRect);
        TryDrawSlot(Part.Underbarrel, underbarrelRect);
    }

    // === Helper ===
    private void TryDrawSlot(Part part, Rect rect) {
        if (!_supportParts.Contains(part)) return;

        var installedTrait = _compDynamicTraits?.GetInstalledTraitFor(part);
        bool clicked;

        if (installedTrait != null) {
            Widgets.DrawOptionBackground(rect, Mouse.IsOver(rect));

            var textureSet = CustomizeWeaponUtility.GetModuleDefFor(installedTrait)
                ?.GetModExtension<TraitModuleExtension>()?.texture;

            if (textureSet != null && !string.IsNullOrEmpty(textureSet.texturePath)) {
                DrawModuleTexture(rect, textureSet);
            } else {
                // fallback: render label
                var originalAnchor = Text.Anchor;
                var originalFont = Text.Font;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(rect, installedTrait.LabelCap);
                Text.Anchor = originalAnchor;
                Text.Font = originalFont;
            }

            clicked = Widgets.ButtonInvisible(rect);
        } else {
            clicked = DrawPartSlot(rect, $"CWF_UI_{part.ToString()}".Translate());
        }

        if (clicked) _onSlotClick?.Invoke(part, installedTrait);
    }

    private static void DrawModuleTexture(Rect rect, TextureSet textureSet) {
        // outline
        if (!string.IsNullOrEmpty(textureSet.outlinePath)) {
            var outlineTexture = ContentFinder<Texture2D>.Get(textureSet.outlinePath, false);
            if (outlineTexture != null) {
                Widgets.DrawTextureFitted(rect, outlineTexture, 1f);
            }
        }

        var mainTexture = ContentFinder<Texture2D>.Get(textureSet.texturePath);
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

        // 使用一个不可见的按钮来检测点击，并返回结果
        return Widgets.ButtonInvisible(rect);
    }
}