using UnityEngine;
using RimWorld;
using Verse;
using CWF.Controllers;
using CWF.Extensions;

namespace CWF.ViewDrawers;

public class HeaderDrawer(Thing weapon, InteractionController controller) {
    private readonly CompRenamable? _compRenamable = weapon.TryGetComp<CompRenamable>();

    public void Draw(in Rect rect) {
        const float iconSize = 40f;
        const float buttonSize = 32f;
        const float gap = 8f;

        // Render weapon icon
        var iconRect = new Rect(rect.x, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize);
        Widgets.ThingIcon(iconRect, weapon);

        // Render search button
        var searchButtonRect = new Rect(rect.xMax - buttonSize, rect.y + (rect.height - buttonSize) / 2f,
            buttonSize, buttonSize);
        TooltipHandler.TipRegion(searchButtonRect, "CWF_UI_BrowseCompatibleModules".Translate());
        if (Widgets.ButtonImage(searchButtonRect.ContractedBy(4f), TexButton.OpenInspector)) {
            Find.WindowStack.Add(new ModuleBrowserWindow(weapon));
        }

        // Render rename button
        var renameButtonRect = new Rect(searchButtonRect.x - gap - buttonSize, searchButtonRect.y,
            buttonSize, buttonSize);
        TooltipHandler.TipRegion(renameButtonRect, "Rename".Translate());
        if (Widgets.ButtonImage(renameButtonRect, TexButton.Rename)) {
            var displayName = _compRenamable?.Nickname ?? weapon.LabelCap;
            var inputModal = new Dialog_TextInput(
                displayName,
                s => {
                    if (_compRenamable == null) return;

                    if (s.Trim().IsNullOrEmpty() || s.Length > 20) {
                        Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    _compRenamable.Nickname = s.Trim();
                },
                "Rename".Translate());
            Find.WindowStack.Add(inputModal);
        }

        // Render action button
        var actionButtonRect = new Rect(renameButtonRect.x - gap - buttonSize, renameButtonRect.y,
            buttonSize, buttonSize);
        if (Widgets.ButtonImage(actionButtonRect.ContractedBy(4f), TexButton.OpenDebugActionsMenu)) {
            var options = new List<FloatMenuOption> {
                new("CWF_UI_ClearAllModules", controller.HasInstalledModules ? controller.ClearAllModules : null)
            };
            Find.WindowStack.Add(new FloatMenu(options));
        }

        // Reserve space
        var labelRect = new Rect(iconRect.xMax + gap, rect.y,
            actionButtonRect.x - (iconRect.xMax + gap) - gap, rect.height);

        // Render name label
        var finalDisplayName = _compRenamable?.Nickname ?? weapon.LabelCap;
        UIKit.WithStyle(() => Widgets.Label(labelRect, finalDisplayName),
            GameFont.Medium, anchor: TextAnchor.MiddleLeft);
    }
}