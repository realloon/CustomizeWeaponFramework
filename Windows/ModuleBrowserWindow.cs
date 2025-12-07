using System.Text;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CWF;

public class ModuleBrowserWindow : Window {
    private readonly Dictionary<PartDef, List<ThingDef>> _groupedModules = new();
    private readonly Dictionary<ThingDef, RecipeDef> _recipeCache = new();
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
            .Where(moduleDef => moduleDef.IsCompatibleWith(weapon.def))
            .ToArray();

        foreach (var moduleDef in compatibleModules) {
            var part = moduleDef.GetModExtension<TraitModuleExtension>().part;

            if (!_groupedModules.ContainsKey(part)) {
                _groupedModules[part] = [];
            }

            _groupedModules[part].Add(moduleDef);
        }

        var relevantModules = compatibleModules.ToHashSet();
        foreach (var recipe in DefDatabase<RecipeDef>.AllDefsListForReading) {
            if (recipe.products.Empty()) continue;

            var productDef = recipe.products[0].thingDef;
            if (relevantModules.Contains(productDef)) {
                _recipeCache.TryAdd(productDef, recipe);
            }
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

            const float buttonSize = 24f;
            var buttonY = rowRect.y + (rowHeight - buttonSize) / 2f;

            var infoButtonRect = new Rect(rowRect.xMax - buttonSize, buttonY, buttonSize, buttonSize);
            var actionButtonRect = new Rect(infoButtonRect.x - buttonSize, buttonY, buttonSize, buttonSize);
            var labelRect = new Rect(rowRect.x + padding, rowRect.y,
                actionButtonRect.x - rowRect.x - padding, rowRect.height);

            var sb = new StringBuilder();
            var traitDef = moduleDef.GetModExtension<TraitModuleExtension>().weaponTraitDef;
            sb.AppendLine(moduleDef.description);
            sb.AppendInNewLine(traitDef.GetTraitEffect());

            TooltipHandler.TipRegion(labelRect, sb.ToString());

            UIKit.WithStyle(() => Widgets.Label(labelRect, moduleDef.LabelCap), anchor: TextAnchor.MiddleLeft);

            currentY += rowHeight;

            if (!Mouse.IsOver(rowRect)) continue;

            // craft
            if (_recipeCache.ContainsKey(moduleDef) &&
                Widgets.ButtonImage(actionButtonRect, TexButton.Add, tooltip: "CWF_UI_Craft".Translate())) {
                TryAddCraftingBill(moduleDef);
            }

            // info
            if (Widgets.ButtonImage(infoButtonRect.ContractedBy(2f),
                    TexButton.Info, tooltip: "DefInfoTip".Translate())) {
                Find.WindowStack.Add(new Dialog_InfoCard(moduleDef));
            }
        }

        Widgets.EndScrollView();
        listing.End();
    }

    private void TryAddCraftingBill(ThingDef moduleDef) {
        if (!_recipeCache.TryGetValue(moduleDef, out var recipe)) return;

        if (recipe == null) {
            Log.Warning("Cannot craft this module.");
            return;
        }

        var bench = Find.CurrentMap.listerBuildings.allBuildingsColonist
            .OfType<IBillGiver>()
            .FirstOrDefault(b => b is Thing thing && (thing.def.AllRecipes?.Contains(recipe) ?? false));

        if (bench == null) {
            Messages.Message("CWF_Message_NoWorkbenchToCraftModule".Translate(moduleDef.Named("MODULE")),
                MessageTypeDefOf.RejectInput, false);
            return;
        }

        var bill = recipe.MakeNewBill();
        if (bill is Bill_Production billProduction) {
            billProduction.repeatMode = BillRepeatModeDefOf.RepeatCount;
            billProduction.repeatCount = 1;
            bench.BillStack.AddBill(bill);
        }

        SoundDefOf.Click.PlayOneShotOnCamera();
        Messages.Message("CWF_Message_BillAdded".Translate(moduleDef.Named("MODULE"), bench.Named("BENCH")),
            new LookTargets((Thing)bench), MessageTypeDefOf.PositiveEvent);
    }
}