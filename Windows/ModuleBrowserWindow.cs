using System.Text;
using UnityEngine;
using Verse;

namespace CWF;

public class ModuleBrowserWindow : Window {
    private readonly Dictionary<PartDef, List<ThingDef>> _groupedModules = new();
    private PartDef? _selectedPart;
    private Vector2 _rightColumnScrollPosition = Vector2.zero;

    public override Vector2 InitialSize => new(550f, 420f);

    public ModuleBrowserWindow(Thing weapon) {
        doCloseX = true;
        closeOnClickedOutside = false;
        draggable = true;
        resizeable = true;
        absorbInputAroundWindow = true;
        forcePause = true;

        var compatibleModules = ModuleDatabase.AllModuleDefs
            .Where(moduleDef => moduleDef.IsCompatibleWith(weapon.def));

        foreach (var moduleDef in compatibleModules) {
            var part = moduleDef.GetModExtension<TraitModuleExtension>().part;

            if (!_groupedModules.ContainsKey(part)) {
                _groupedModules[part] = [];
            }

            _groupedModules[part].Add(moduleDef);
        }

        _groupedModules = _groupedModules
            .OrderBy(kvp => kvp.Key.LabelCap.ToString())
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public override void DoWindowContents(Rect inRect) {
        const float leftColumnWidth = 128f;
        const float columnGap = 16f;

        var leftRect = new Rect(inRect.x, inRect.y, leftColumnWidth, inRect.height);
        var rightRect = new Rect(leftRect.xMax + columnGap, inRect.y,
            inRect.width - leftColumnWidth - columnGap, inRect.height);

        DrawLeftColumn(leftRect);
        DrawRightColumn(rightRect);
    }

    private void DrawLeftColumn(in Rect rect) {
        const float itemGap = 8f;
        var listing = new Listing_Standard();

        listing.Begin(rect);

        UIKit.WithStyle(() => listing.Label("CWF_UI_Parts".Translate()), GameFont.Medium);

        listing.Gap(itemGap);

        if (listing.RadioButton("CWF_UI_All".Translate(), _selectedPart == null)) {
            _selectedPart = null;
        }

        listing.Gap(itemGap);

        if (_groupedModules.NullOrEmpty()) {
            listing.End();
            return;
        }

        foreach (var part in _groupedModules.Keys) {
            if (listing.RadioButton(part.LabelCap, _selectedPart == part)) {
                _selectedPart = part;
            }

            listing.Gap(itemGap);
        }

        listing.End();
    }

    private void DrawRightColumn(in Rect rect) {
        var listing = new Listing_Standard();
        listing.Begin(rect);

        const float padding = 8f;
        const float titleHeight = 32f;
        var titleRect = listing.GetRect(titleHeight);
        var paddedTitleRect = new Rect(titleRect.x + padding, titleRect.y, titleRect.width - padding, titleRect.height);

        UIKit.WithStyle(() => Widgets.Label(paddedTitleRect, "CWF_UI_CompatibleModules".Translate()), GameFont.Medium);

        var modulesToShow = _selectedPart switch {
            null => _groupedModules.Values.SelectMany(list => list).ToList(),
            _ => _groupedModules.GetValueOrDefault(_selectedPart) ?? []
        };

        if (modulesToShow.Empty()) {
            var noModuleLabelRect = listing.GetRect(Text.LineHeight);
            noModuleLabelRect.x += padding;
            Widgets.Label(noModuleLabelRect, "CWF_UI_NoCompatibleModules".Translate());
            listing.End();
            return;
        }

        const float rowHeight = 32f;

        var viewHeight = rect.height - listing.CurHeight;
        var viewRect = new Rect(0, listing.CurHeight, rect.width, viewHeight);

        var contentHeight = modulesToShow.Count * rowHeight;
        var contentRect = new Rect(0, 0, viewRect.width - 16f, contentHeight);

        Widgets.BeginScrollView(viewRect, ref _rightColumnScrollPosition, contentRect);

        var currentY = 0f;

        foreach (var moduleDef in modulesToShow.OrderBy(m => m.LabelCap.ToString())) {
            var rowRect = new Rect(contentRect.x, currentY, contentRect.width, rowHeight);
            Widgets.DrawHighlightIfMouseover(rowRect);
            var traitDef = moduleDef.GetModExtension<TraitModuleExtension>().weaponTraitDef;
            var sb = new StringBuilder();
            sb.AppendLine(moduleDef.description);
            sb.AppendInNewLine(traitDef.GetTraitEffect());
            TooltipHandler.TipRegion(rowRect, sb.ToString());

            // todo: modified calculation
            var labelRect = new Rect(rowRect.x + padding, rowRect.y, rowRect.width - 100f, rowRect.height);
            UIKit.WithStyle(() => Widgets.Label(labelRect, moduleDef.LabelCap), anchor: TextAnchor.MiddleLeft);

            var infoButtonRect = new Rect(rowRect.xMax - 32f, rowRect.y + (rowRect.height - 32f) / 2, rowHeight,
                rowHeight);
            if (Widgets.ButtonImage(infoButtonRect.ContractedBy(4f), TexButton.Info)) {
                Find.WindowStack.Add(new Dialog_InfoCard(moduleDef));
            }

            currentY += rowHeight;
        }

        Widgets.EndScrollView();
        listing.End();
    }
}