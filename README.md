# Visualised Markup (VB)

A recursive RAD IDE that defines its own UI using a flat-file markup language.

## What It Is

Visualised Markup is a rapid application development environment where **the IDE itself is built using VML** (Visualised Markup Language) - a flat-file format that defines user interfaces and behavior. If VML can build this IDE, it can build anything.

## Paradigm Shifts

### 1. Recursive Self-Definition
The designer, options dialogs, script editor - all defined in VML files. The technology proves itself by using itself. No bootstrap paradox: a minimal C# kernel loads the first VML file, which defines everything else.

### 2. Embedded Script Objects
Event handlers aren't compiled code - they're @Script objects embedded in VML.
Scripts execute in bash, python, node - your choice. Multi-language by design.

### 3. Flat-File Simplicity
No XML hierarchy. No JSON nesting. Just properties.
Parse with split('='). Edit with vi. Diff with git.

### 4. PropertyStore Bridge
SQLite database syncs C# control properties with script environment.
Scripts manipulate UI without FFI, marshalling, or bindings. Just SQL.

### 5. Zero-Friction Distribution
Applications are VML files + a generic runtime. No compilation. No deployment. No dependencies beyond .NET/Avalonia.

VML files are:
- **Human-readable** - Open in any editor
- **Version-controllable** - Text-based, diffable
- **Self-documenting** - Properties are obvious
- **Hot-reloadable** - Edit and refresh

### 6. Self-Replicating Architecture
VML definitions stored in SQLite. The IDE can export itself, rebuild itself, version its own UI.
The app distribution model inverts: instead of shipping binaries, ship VML + generic runtime.

## Why This Matters

**Traditional RAD:** Visual designer generates code → compiles to binary → deploy binary → UI changes require recompile

**Visualised Markup:** Visual designer generates VML → interpreted at runtime → deploy VML file → UI changes are text edits

## Getting Started

Build: dotnet build
Run: ./bin/Debug/net9.0/VB

The designer opens. Defined in designer.vml. Edit it while running.

## License

Dual-licensed:
- **Free (AGPLv3):** Open source projects
- **Commercial:** Proprietary/closed-source projects

See LICENSE.md for details.
