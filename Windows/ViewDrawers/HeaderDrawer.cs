using RimWorld;
using UnityEngine;
using Verse;

namespace CWF.ViewDrawers;

public class HeaderDrawer {
    private readonly Thing _weapon;
    private readonly CompRenamable _compRenamable;

    public HeaderDrawer(Thing weapon) {
        _weapon = weapon;
        _compRenamable = weapon.TryGetComp<CompRenamable>();
    }

    public void Draw(Rect rect) {
        const float iconSize = 40f;
        const float buttonSize = 32f;
        const float gap = 8f;

        // Render Icon
        var iconRect = new Rect(rect.x, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize);
        Widgets.ThingIcon(iconRect, _weapon);

        // Reserve space
        var labelRect = new Rect(iconRect.xMax + gap, rect.y,
            rect.width - iconSize - buttonSize * 2 - gap * 3,
            rect.height);

        // Render name label
        var originalAnchor = Text.Anchor;
        var originalFont = Text.Font;
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Medium;
        var displayName = _compRenamable?.Nickname ?? _weapon.LabelCap;
        Widgets.Label(labelRect, displayName);
        Text.Anchor = originalAnchor;
        Text.Font = originalFont;

        // Render rename button
        var renameButtonRect = new Rect(labelRect.xMax + gap, rect.y + (rect.height - buttonSize) / 2f,
            buttonSize, buttonSize);
        TooltipHandler.TipRegion(renameButtonRect, "Rename".Translate());
        if (Widgets.ButtonImage(renameButtonRect, TexButton.Rename)) {
            var inputModal = new Dialog_TextInput(
                displayName,
                s => {
                    if (_compRenamable == null) return;

                    if (s.Trim().NullOrEmpty() || s.Length > 20) {
                        Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    _compRenamable.Nickname = s.Trim();
                },
                "Rename".Translate());
            Find.WindowStack.Add(inputModal);
        }
    }
}