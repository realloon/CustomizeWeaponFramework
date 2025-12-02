using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CWF.Controllers;

/// <summary>
/// 工具类：用于在工作台添加配件制作订单
/// </summary>
public static class AttachmentCraftingUtility {
    private const string WorkbenchDefName = "CWF_Workbench";
    
    private static ThingDef? _workbenchDef;
    private static List<RecipeDef>? _cachedWorkbenchRecipes;
    private static readonly Dictionary<ThingDef, RecipeDef?> _recipeCache = new();
    
    private static ThingDef? GetWorkbenchDef() {
        if (_workbenchDef == null) {
            _workbenchDef = DefDatabase<ThingDef>.GetNamed(WorkbenchDefName, false);
            if (_workbenchDef == null) {
                Log.Warning($"[CWF] 未找到工作台定义 '{WorkbenchDefName}'");
            }
        }
        return _workbenchDef;
    }
    
    private static List<RecipeDef> GetWorkbenchRecipes() {
        if (_cachedWorkbenchRecipes != null) {
            return _cachedWorkbenchRecipes;
        }
        
        var workbenchDef = GetWorkbenchDef();
        if (workbenchDef == null) {
            _cachedWorkbenchRecipes = new List<RecipeDef>();
            return _cachedWorkbenchRecipes;
        }
        
        // 优先使用工作台定义的AllRecipes（最快路径）
        if (workbenchDef.AllRecipes != null && workbenchDef.AllRecipes.Count > 0) {
            _cachedWorkbenchRecipes = workbenchDef.AllRecipes.ToList();
            return _cachedWorkbenchRecipes;
        }
        
        // 备用方案：过滤所有配方，找出指定了CWF_Workbench的配方
        _cachedWorkbenchRecipes = DefDatabase<RecipeDef>.AllDefs
            .Where(recipe => recipe.recipeUsers != null && recipe.recipeUsers.Contains(workbenchDef))
            .ToList();
        
        return _cachedWorkbenchRecipes;
    }
    
    private static RecipeDef? FindRecipeForModule(ThingDef moduleDef) {
        if (moduleDef == null) {
            return null;
        }
        
        // 检查缓存，但需要验证配方是否仍然可用（科技解锁状态可能已改变）
        if (_recipeCache.TryGetValue(moduleDef, out var cachedRecipe)) {
            if (cachedRecipe != null) {
                // 再次验证可用性（科技解锁状态可能已改变）
                if (cachedRecipe.AvailableNow) {
                    return cachedRecipe;
                }
                // 缓存无效，清除并重新查找
                _recipeCache.Remove(moduleDef);
            } else {
                // 即使缓存为null，也要重新查找，因为科技解锁状态可能已改变
                _recipeCache.Remove(moduleDef);
            }
        }
        
        // 方法1：如果配件定义有AllRecipes属性，直接使用
        if (moduleDef.AllRecipes != null && moduleDef.AllRecipes.Count > 0) {
            foreach (var recipe in moduleDef.AllRecipes) {
                if (recipe.ProducedThingDef == moduleDef && recipe.AvailableNow) {
                    _recipeCache[moduleDef] = recipe;
                    return recipe;
                }
            }
        }
        
        // 方法2：只搜索CWF_Workbench的配方（优化）
        var workbenchRecipes = GetWorkbenchRecipes();
        foreach (var recipe in workbenchRecipes) {
            if (recipe.ProducedThingDef != moduleDef) {
                continue;
            }
            
            if (!recipe.AvailableNow) {
                continue;
            }
            
            _recipeCache[moduleDef] = recipe;
            return recipe;
        }
        
        // 缓存null结果，避免重复查找
        _recipeCache[moduleDef] = null;
        return null;
    }
    
    /// <summary>
    /// 查找所有能制作指定配件的工作台和配方
    /// </summary>
    public static List<(Building_WorkTable workTable, RecipeDef recipe)> 
        FindWorkTablesForModule(ThingDef moduleDef, Map? map = null) {
        var results = new List<(Building_WorkTable, RecipeDef)>();
        
        if (moduleDef == null) {
            return results;
        }
        
        if (map == null) {
            map = Find.CurrentMap;
        }
        
        if (map == null) {
            return results;
        }
        
        var workbenchDef = GetWorkbenchDef();
        if (workbenchDef == null) {
            return results;
        }
        
        var recipe = FindRecipeForModule(moduleDef);
        if (recipe == null) {
            return results;
        }
        
        // 双重检查配方可用性（科技解锁验证）
        if (!recipe.AvailableNow) {
            return results;
        }
        
        // 查找地图上的CWF_Workbench实例
        foreach (var workTable in map.listerBuildings.AllBuildingsColonistOfDef(workbenchDef)) {
            if (workTable is not Building_WorkTable workBench) {
                continue;
            }
            
            if (!recipe.AvailableOnNow(workBench, null)) {
                continue;
            }
            
            results.Add((workBench, recipe));
        }
        
        return results;
    }
    
    /// <summary>
    /// 在工作台添加制作订单
    /// </summary>
    public static bool AddCraftingBill(
        Building_WorkTable workTable, 
        RecipeDef recipe, 
        ThingDef moduleDef,
        string weaponName,
        bool showMessage = true) {

        if (workTable == null || recipe == null || moduleDef == null) {
            Log.Warning("[CWF] AddCraftingBill: 参数验证失败");
            return false;
        }
        
        var newBill = BillUtility.MakeNewBill(recipe, null);
        
        if (newBill is Bill_Production productionBill) {
            productionBill.RenamableLabel = "CWF_UI_CraftingBillLabel"
                .Translate(weaponName.Named("WEAPON"), moduleDef.LabelCap.Named("MODULE"));
        }
        
        workTable.billStack.AddBill(newBill);
        
        if (showMessage) {
            Messages.Message(
                "CWF_Message_CraftingBillAdded"
                    .Translate(workTable.LabelCap.Named("WORKTABLE"), moduleDef.LabelCap.Named("MODULE")),
                MessageTypeDefOf.TaskCompletion,
                false
            );
        }
        
        return true;
    }
    
    /// <summary>
    /// 为指定配件查找工作台并添加订单（自动选择第一个可用工作台）
    /// </summary>
    public static bool AddCraftingBillForModule(ThingDef moduleDef, string weaponName, Map? map = null) {
        if (moduleDef == null) {
            return false;
        }
        
        var workTables = FindWorkTablesForModule(moduleDef, map);
        
        if (workTables.Count == 0) {
            Messages.Message(
                "CWF_Message_NoWorkTableForModule"
                    .Translate(moduleDef.LabelCap.Named("MODULE")),
                MessageTypeDefOf.RejectInput,
                false
            );
            return false;
        }
        
        var (workTable, recipe) = workTables[0];
        return AddCraftingBill(workTable, recipe, moduleDef, weaponName);
    }
}
