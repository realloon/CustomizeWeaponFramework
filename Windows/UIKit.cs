using UnityEngine;
using Verse;

namespace CWF;

internal static class UIKit {
    internal static void WithStyle(Action drawAction, GameFont font = GameFont.Small, Color? color = null,
        TextAnchor anchor = TextAnchor.UpperLeft) {
        var originalFont = Text.Font;
        var originalColor = GUI.color;
        var originalAnchor = Text.Anchor;

        Text.Font = font;
        GUI.color = color ?? Color.white;
        Text.Anchor = anchor;

        drawAction();

        Text.Font = originalFont;
        GUI.color = originalColor;
        Text.Anchor = originalAnchor;
    }
}