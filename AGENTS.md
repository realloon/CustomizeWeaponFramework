# AGENTS.md

Customize Weapon Framework (CWF) is a RimWorld Mod.

## Note

- C#：`Source/`
- Entry: `Source/CustomizeWeaponFramework.cs`
- Build: `dotnet build Source/CWF.slnx`
- Dependencies: `Krafs.Rimworld.Ref`、`Lib.Harmony.Ref`

## Architecture

### Component-Based

- Core functionality lives in ThingComps (`Source/ThingComps/`) classes
- Harmony Patching: Runtime method interception via Prefix, Postfix, and Transpiler patches in `Source/HarmonyPatches/`. Naming convention: `{PatchType}_{TargetClass}_{MethodName}.cs`

### MVC for UI

- Models: `Source/Data/` classes (SpecDatabase, ModuleDatabase)
- Controllers: `Source/Controllers/` classes
- Views: `Source/Windows/` classes
