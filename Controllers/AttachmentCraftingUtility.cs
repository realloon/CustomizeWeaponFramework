using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CWF.Controllers;

/// <summary>
/// 工具类：用于在工作台添加配件制作订单
/// </summary>
public static class AttachmentCraftingUtility {
    /// <summary>
    /// CWF工作台的defName常量
    /// </summary>
    private const string WorkbenchDefName = "CWF_Workbench";
    
    /// <summary>
    /// 获取CWF工作台定义（缓存以避免重复查找）
    /// </summary>
    private static ThingDef? _workbenchDef;
    
    private static ThingDef? GetWorkbenchDef() {
        if (_workbenchDef == null) {
            _workbenchDef = DefDatabase<ThingDef>.GetNamed(WorkbenchDefName, false);
            if (_workbenchDef != null) {
                Log.Message($"[CWF] GetWorkbenchDef: 成功获取工作台定义 '{WorkbenchDefName}'");
            } else {
                Log.Warning($"[CWF] GetWorkbenchDef: 未找到工作台定义 '{WorkbenchDefName}'");
            }
        }
        return _workbenchDef;
    }
    
    /// <summary>
    /// 查找能制作指定配件的配方
    /// </summary>
    /// <param name="moduleDef">配件定义</param>
    /// <returns>配方定义，如果未找到则返回null</returns>
    private static RecipeDef? FindRecipeForModule(ThingDef moduleDef) {
        if (moduleDef == null) {
            Log.Warning("[CWF] FindRecipeForModule: moduleDef 为 null");
            return null;
        }
        
        Log.Message($"[CWF] FindRecipeForModule: 开始查找配件 '{moduleDef.defName}' 的配方");
        
        // 方法1：如果配件定义有AllRecipes属性，直接使用
        if (moduleDef.AllRecipes != null) {
            Log.Message($"[CWF] FindRecipeForModule: 使用方法1 - 检查 moduleDef.AllRecipes (共 {moduleDef.AllRecipes.Count} 个配方)");
            foreach (var recipe in moduleDef.AllRecipes) {
                if (recipe.ProducedThingDef == moduleDef && recipe.AvailableNow) {
                    Log.Message($"[CWF] FindRecipeForModule: 方法1成功 - 找到配方 '{recipe.defName}'");
                    return recipe;
                }
            }
            Log.Message("[CWF] FindRecipeForModule: 方法1未找到可用配方，尝试方法2");
        } else {
            Log.Message("[CWF] FindRecipeForModule: moduleDef.AllRecipes 为 null，使用方法2");
        }
        
        // 方法2：遍历所有配方，查找能制作该配件的配方
        var allRecipes = DefDatabase<RecipeDef>.AllDefs;
        Log.Message($"[CWF] FindRecipeForModule: 使用方法2 - 遍历所有配方 (共 {allRecipes.Count()} 个)");
        int checkedCount = 0;
        foreach (var recipe in allRecipes) {
            // 检查配方是否制作目标配件
            if (recipe.ProducedThingDef != moduleDef) {
                continue;
            }
            
            checkedCount++;
            
            // 检查配方是否可用
            if (!recipe.AvailableNow) {
                Log.Message($"[CWF] FindRecipeForModule: 配方 '{recipe.defName}' 不可用 (AvailableNow=false)");
                continue;
            }
            
            // 检查配方是否指定了CWF_Workbench作为工作台
            var workbenchDef = GetWorkbenchDef();
            if (recipe.recipeUsers != null && recipe.recipeUsers.Contains(workbenchDef)) {
                Log.Message($"[CWF] FindRecipeForModule: 方法2成功 - 找到配方 '{recipe.defName}' (检查了 {checkedCount} 个匹配配方)");
                return recipe;
            } else {
                Log.Message($"[CWF] FindRecipeForModule: 配方 '{recipe.defName}' 未指定 CWF_Workbench 作为工作台");
            }
        }
        
        Log.Warning($"[CWF] FindRecipeForModule: 未找到配件 '{moduleDef.defName}' 的可用配方");
        return null;
    }
    
    /// <summary>
    /// 查找所有能制作指定配件的工作台和配方
    /// </summary>
    /// <param name="moduleDef">配件定义</param>
    /// <param name="map">目标地图（如果为null则使用当前地图）</param>
    /// <returns>工作台和配方的配对列表</returns>
    public static List<(Building_WorkTable workTable, RecipeDef recipe)> 
        FindWorkTablesForModule(ThingDef moduleDef, Map? map = null) {
        var results = new List<(Building_WorkTable, RecipeDef)>();
        
        Log.Message($"[CWF] FindWorkTablesForModule: 开始查找配件 '{moduleDef?.defName}' 的工作台");
        
        if (moduleDef == null) {
            Log.Warning("[CWF] FindWorkTablesForModule: moduleDef 为 null");
            return results;
        }
        
        if (map == null) {
            map = Find.CurrentMap;
            Log.Message("[CWF] FindWorkTablesForModule: map 为 null，使用当前地图");
        }
        
        if (map == null) {
            Log.Warning("[CWF] FindWorkTablesForModule: 无法获取地图");
            return results;
        }
        
        Log.Message($"[CWF] FindWorkTablesForModule: 使用地图 '{map.uniqueID}'");
        
        // 获取CWF工作台定义
        var workbenchDef = GetWorkbenchDef();
        if (workbenchDef == null) {
            Log.Warning("[CWF] FindWorkTablesForModule: 无法获取工作台定义");
            return results;
        }
        
        // 查找能制作该配件的配方
        var recipe = FindRecipeForModule(moduleDef);
        if (recipe == null) {
            Log.Warning("[CWF] FindWorkTablesForModule: 无法找到配方");
            return results;
        }
        
        Log.Message($"[CWF] FindWorkTablesForModule: 已找到配方 '{recipe.defName}'，开始查找地图上的工作台实例");
        
        // 直接查找地图上的CWF工作台实例（避免遍历所有工作台）
        var allWorkbenches = map.listerBuildings.AllBuildingsColonistOfDef(workbenchDef).ToList();
        Log.Message($"[CWF] FindWorkTablesForModule: 地图上找到 {allWorkbenches.Count} 个 CWF_Workbench 工作台");
        
        int checkedCount = 0;
        foreach (var workTable in allWorkbenches) {
            checkedCount++;
            if (workTable is not Building_WorkTable workBench) {
                Log.Message($"[CWF] FindWorkTablesForModule: 工作台 #{checkedCount} 不是 Building_WorkTable 类型，跳过");
                continue;
            }
            
            // 检查配方是否可以在该工作台上使用
            if (!recipe.AvailableOnNow(workBench, null)) {
                Log.Message($"[CWF] FindWorkTablesForModule: 工作台 '{workBench.Label}' (位置: {workBench.Position}) 配方不可用，跳过");
                continue;
            }
            
            Log.Message($"[CWF] FindWorkTablesForModule: 找到可用工作台 '{workBench.Label}' (位置: {workBench.Position})");
            results.Add((workBench, recipe));
        }
        
        Log.Message($"[CWF] FindWorkTablesForModule: 完成查找，共找到 {results.Count} 个可用工作台");
        return results;
    }
    
    /// <summary>
    /// 在工作台添加制作订单
    /// </summary>
    /// <param name="workTable">工作台</param>
    /// <param name="recipe">配方</param>
    /// <param name="moduleDef">配件定义</param>
    /// <param name="weaponName">武器名称（用于订单命名）</param>
    /// <param name="showMessage">是否显示提示消息</param>
    /// <returns>是否成功添加</returns>
    public static bool AddCraftingBill(
        Building_WorkTable workTable, 
        RecipeDef recipe, 
        ThingDef moduleDef,
        string weaponName,
        bool showMessage = true) {

        Log.Message($"[CWF] AddCraftingBill: 开始添加订单 - 工作台: '{workTable?.Label}', 配方: '{recipe?.defName}', 配件: '{moduleDef?.defName}', 武器: '{weaponName}'");

        if (workTable == null || recipe == null || moduleDef == null) {
            Log.Warning($"[CWF] AddCraftingBill: 参数验证失败 - workTable={workTable != null}, recipe={recipe != null}, moduleDef={moduleDef != null}");
            return false;
        }
        
        // 创建新订单
        var newBill = BillUtility.MakeNewBill(recipe, null);
        Log.Message($"[CWF] AddCraftingBill: 已创建订单，类型: {newBill.GetType().Name}");
        
        // 自定义订单名称：为「<武器名称>」制作的「<配件名称>」
        if (newBill is Bill_Production productionBill) {
            productionBill.RenamableLabel = "CWF_UI_CraftingBillLabel"
                .Translate(weaponName.Named("WEAPON"), moduleDef.LabelCap.Named("MODULE"));
            Log.Message($"[CWF] AddCraftingBill: 订单标签已设置: '{productionBill.RenamableLabel}'");
        }
        
        // 添加订单到工作台
        workTable.billStack.AddBill(newBill);
        Log.Message($"[CWF] AddCraftingBill: 订单已添加到工作台 '{workTable.Label}' (位置: {workTable.Position})，当前订单数: {workTable.billStack.Count}");
        
        // 显示提示消息
        if (showMessage) {
            Messages.Message(
                "CWF_Message_CraftingBillAdded"
                    .Translate(workTable.LabelCap.Named("WORKTABLE"), moduleDef.LabelCap.Named("MODULE")),
                MessageTypeDefOf.TaskCompletion,
                false
            );
        }
        
        Log.Message("[CWF] AddCraftingBill: 订单添加成功");
        return true;
    }
    
    /// <summary>
    /// 为指定配件查找工作台并添加订单（自动选择第一个可用工作台）
    /// </summary>
    /// <param name="moduleDef">配件定义</param>
    /// <param name="weaponName">武器名称（用于订单命名）</param>
    /// <param name="map">目标地图</param>
    /// <returns>是否成功添加</returns>
    public static bool AddCraftingBillForModule(ThingDef moduleDef, string weaponName, Map? map = null) {
        Log.Message($"[CWF] AddCraftingBillForModule: 入口方法 - 配件: '{moduleDef?.defName}', 武器: '{weaponName}'");
        
        if (moduleDef == null) {
            Log.Warning("[CWF] AddCraftingBillForModule: moduleDef 为 null");
            return false;
        }
        
        var workTables = FindWorkTablesForModule(moduleDef, map);
        
        if (workTables.Count == 0) {
            Log.Warning($"[CWF] AddCraftingBillForModule: 未找到可用工作台，无法添加订单");
            Messages.Message(
                "CWF_Message_NoWorkTableForModule"
                    .Translate(moduleDef.LabelCap.Named("MODULE")),
                MessageTypeDefOf.RejectInput,
                false
            );
            return false;
        }
        
        Log.Message($"[CWF] AddCraftingBillForModule: 找到 {workTables.Count} 个可用工作台，使用第一个");
        
        // 使用第一个找到的工作台
        var (workTable, recipe) = workTables[0];
        Log.Message($"[CWF] AddCraftingBillForModule: 选择工作台 '{workTable.Label}' (位置: {workTable.Position})，配方 '{recipe.defName}'");
        
        var result = AddCraftingBill(workTable, recipe, moduleDef, weaponName);
        Log.Message($"[CWF] AddCraftingBillForModule: 订单添加结果: {result}");
        return result;
    }
}

