# Customize Weapon Framework

**Customize Weapon Framework (CWF)** is a weapon customization framework Mod designed for RimWorld. It provides a powerful underlying system that allows developers to create highly configurable weapons and add deep customization features to existing ones.

CWF aims to be an extensible platform, enabling other Mod authors to easily introduce new weapon parts, modules, and customization options to RimWorld.

## Project Structure

The following are the key directories of the CWF project and their functions:

- **`CompProperties/`**: Component property class definitions.
- **`Controllers/`**: Game logic and event controllers.
- **`Data/`**: Module and specification data management.
- **`DefModExtensions/`**: Defs extension class definitions.
- **`Defs/`**: C# implementations for Mod Defs.
- **`Extensions/`**: Utility extension methods.
- **`HarmonyPatches/`**: Harmony patch implementations.
- **`JobDrivers/`** and **`JobGivers/`**: Weapon modification quest system.
- **`StatParts/`**: Stat modifiers.
- **`ThingComps/`**: Weapon functionality components.
- **`Windows/`**: Custom UI panels.

## Development Environment

1. **Setup**: Clone this repository into your development environment.
2. **Dependency**: All project dependencies are managed via NuGet packages.
4. **Build**: Please ensure to modify the project's compilation output path to your Mod folder.