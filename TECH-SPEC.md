# Visualised Markup - Technical Specification

Version: 1.0  
Last Updated: 2025-11-08  
Status: Active Development

## Executive Summary

Visualised Markup (VB) is a recursive RAD IDE built using Avalonia UI that employs a flat-file markup language (VML) to define user interfaces and behavior. The system is unique in that **the IDE itself is defined in VML**, demonstrating the power and flexibility of the markup language.

## Core Innovation

Traditional RAD tools generate source code from visual designs. VML inverts this: visual designs ARE the source. No compilation step. No code generation. The markup is the application.

## Architecture

### System Overview
```
┌───────────────────────────────────────────────────────┐
│                    Application Layer                   │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│  │ MainWindow   │ │DesignerWindow│ │ Options      │ │
│  │   (C#)       │ │    (C#)      │ │   (VML)      │ │
│  └──────────────┘ └──────────────┘ └──────────────┘ │
└───────────────────────────────────────────────────────┘
                          ↓
┌───────────────────────────────────────────────────────┐
│                     VML Layer                          │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│  │VmlLoader     │ │ VmlUiBuilder │ │ScriptRegistry│ │
│  │ (Parser)     │ │ (Generator)  │ │              │ │
│  └──────────────┘ └──────────────┘ └──────────────┘ │
└───────────────────────────────────────────────────────┘
                          ↓
┌───────────────────────────────────────────────────────┐
│                    Runtime Layer                       │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│  │PropertyStore │ │ScriptHandler │ │ControlManager│ │
│  │  (SQLite)    │ │  (Multi-Lang)│ │              │ │
│  └──────────────┘ └──────────────┘ └──────────────┘ │
└───────────────────────────────────────────────────────┘
```

### Core Components

#### 1. VML Parser (VmlWindowLoader.cs)
**Responsibility**: Parse VML flat files into window definitions and register @Script objects

**Input Format**:
```vml
# Window properties
Title=My Window
Width=800

# Control with indexed instances
Button.0.Content=Click Me
Button.0.OnClick=MyScript

# Script object
@Script MyScript
Interpreter=bash
Content=<<EOF
  echo "Clicked"
EOF
```

**Process**:
1. Parse flat key=value properties
2. Identify @Script objects
3. Register scripts in ScriptRegistry
4. Build control hierarchy
5. Wire event handlers to scripts

**Key Features**:
- Comment support (# prefix)
- Multi-line content via heredoc (<<EOF)
- @Object definitions
- Whitespace handling
- Error recovery

#### 2. UI Builder (VmlUiBuilder.cs)
**Responsibility**: Transform property dictionaries into Avalonia controls

**Supported Controls**:
- Layout: Border, StackPanel, Grid, DockPanel, ScrollViewer
- Input: Button, TextBox, CheckBox, RadioButton, ComboBox, ListBox
- Display: TextBlock, Separator, Expander
- All standard Avalonia controls via reflection

**Property Application**:
- Type conversion via reflection
- Nested property setting (e.g., "Button.Background")
- Collection properties (e.g., Grid.ColumnDefinitions)
- Attached properties (e.g., Grid.Column, DockPanel.Dock)

#### 3. Script Registry (ScriptRegistry.cs)
**Responsibility**: Central registry for @Script objects

**API**:
```csharp
ScriptRegistry.Register("MyScript", scriptContent, "bash");
VmlScript? script = ScriptRegistry.Get("MyScript");
```

**Script Object Structure**:
```csharp
public class VmlScript
{
    public string Name { get; set; }
    public string Content { get; set; }
    public string Interpreter { get; set; } // bash, python, node, etc
}
```

Scripts are loaded once during VML parsing and cached for the window's lifetime.

#### 4. Property Store (PropertyStore.cs)
**Responsibility**: SQLite-based property synchronization between C# and scripts

**Schema**:
```sql
CREATE TABLE properties (
    control_name TEXT NOT NULL,
    property_name TEXT NOT NULL,
    property_value TEXT,
    PRIMARY KEY (control_name, property_name)
);
```

**C# API**:
```csharp
PropertyStore.Set("Button_1", "Content", "Click Me");
string? value = PropertyStore.Get("Button_1", "Content");
PropertyStore.SyncControl(control); // Sync all properties
```

**Script Access**:
```bash
vb-get Button_1.Content
vb-set TextBox_1.Text "Hello"
```

Scripts read/write control properties via simple bash commands that query SQLite.

#### 5. Script Handler (ScriptHandler.cs)
**Responsibility**: Execute scripts in multiple languages

**Supported Interpreters**:
- bash (default)
- python
- node (JavaScript)
- ruby
- perl
- C# compilation (planned)

**Execution Model**:
```
Event Fired
     ↓
VmlWindowLoader.ExecuteScript()
     ↓
Fork Process (interpreter)
     ↓
Pass Environment Variables:
  • CONTROL_NAME
  • EVENT_TYPE
  • CONTROL_TYPE
     ↓
Execute Script Content
     ↓
Capture stdout/stderr
     ↓
Return exit code
```

**Security**: Scripts execute with user permissions. No sandboxing currently implemented.

#### 6. Auto-Naming System
**Responsibility**: Generate unique control names automatically

**Algorithm**:
```csharp
Dictionary<string, int> controlCounters;

string GenerateName(string controlType)
{
    if (!controlCounters.ContainsKey(controlType))
        controlCounters[controlType] = 0;
    
    controlCounters[controlType]++;
    return $"{controlType}_{controlCounters[controlType]}";
}
```

**Examples**: Button_1, Button_2, TextBox_1, StackPanel_1

### VML Specification

#### Syntax Rules

**Window Properties**:
```vml
Title=My Application
Width=800
Height=600
```

**Control Definition**:
```vml
@ControlType ControlName
Property=Value
Parent=ParentName
```

**Indexed Controls** (multiple of same type):
```vml
Button.0.Content=First
Button.1.Content=Second
```

**Script Objects**:
```vml
@Script ScriptName
Interpreter=bash|python|node|ruby|perl
Content=<<EOF
  script content here
EOF
```

**Event Handlers**:
```vml
Button.0.OnClick=ScriptName
ListBox.0.OnSelectionChanged=AnotherScript
```

**Layout Properties**:
```vml
Border.Dock=Top
Grid.Column=1
Grid.Row=2
StackPanel.Orientation=Horizontal
```

#### Complete Example
```vml
# Options Window
Title=Options
Width=600
Height=400

@Grid MainGrid
ColumnDefinitions=200,*

@ListBox categoryList
Grid.Column=0
OnSelectionChanged=TogglePanel

  @Script TogglePanel
  Interpreter=bash
  Content=<<EOF
    # Get selected index
    idx=$(vb-get categoryList.SelectedIndex)
    
    # Toggle panel visibility
    vb-set generalPanel.IsVisible "$([ $idx -eq 0 ] && echo True || echo False)"
    vb-set advancedPanel.IsVisible "$([ $idx -eq 1 ] && echo True || echo False)"
  EOF

@ListBoxItem cat1
Parent=categoryList
Content=General

@ListBoxItem cat2
Parent=categoryList
Content=Advanced

@StackPanel generalPanel
Grid.Column=1
IsVisible=True

@TextBlock label1
Parent=generalPanel
Text=General Settings

@StackPanel advancedPanel
Grid.Column=1
IsVisible=False

@TextBlock label2
Parent=advancedPanel
Text=Advanced Settings
```

### Design Patterns

#### 1. Recursive Self-Definition
The IDE's own UI is defined in VML:
```
designer.vml          → Main designer window layout
options-window.vml    → Options dialog with @Script panels
visual-script-editor.vml → Script editor
```

**Benefit**: Proves VML is powerful enough for complex UIs. Dogfooding ensures quality.

#### 2. @Script Objects Pattern
Reusable behavior as first-class objects:
```vml
@Script ValidationScript
Interpreter=python
Content=<<EOF
  import re
  text = property_store.get("emailBox", "Text")
  valid = bool(re.match(r'^[\w\.-]+@[\w\.-]+\.\w+$', text))
  property_store.set("saveButton", "IsEnabled", str(valid))
EOF
```

#### 3. Property Synchronization
Bidirectional bridge via SQLite:
```
C# Control Property → PropertyStore.Set() → SQLite
                                              ↓
                                           vb-get
                                              ↓
Script reads value ← vb-get query ← SQLite
Script writes value → vb-set command → SQLite
                                              ↓
                                       PropertyStore.Get()
                                              ↓
                                    C# Control updates
```

### Data Flow

#### Application Startup
```
1. MainWindow.ctor()
2. PropertyStore.Initialize(/tmp/vb-runtime.db)
3. Load designer.vml
4. VmlWindowLoader.LoadWindow()
   a. Parse VML properties
   b. Register @Script objects
   c. VmlUiBuilder.BuildFromVml()
5. Display UI
```

#### VML Window Loading
```
1. User clicks File → Options
2. MainWindow.HandleOptions()
3. VmlWindowLoader.LoadWindow("options-window.vml")
   a. Parse flat-file VML
   b. Extract @Script definitions
   c. ScriptRegistry.Register() for each script
   d. Build window from properties
   e. Wire events to scripts
4. ShowDialog()
```

#### Event Execution
```
1. User clicks button
2. Button.Click event fires
3. VmlWindowLoader.ExecuteScript()
4. Retrieve script from ScriptRegistry
5. ScriptHandler.Execute()
   a. Fork interpreter process
   b. Pass environment variables
   c. Execute script content
6. Script uses vb-get/vb-set
   a. Query PropertyStore SQLite
   b. Read/write control properties
7. UI updates based on property changes
```

### Performance

#### Benchmarks (Typical Hardware)
- **VML Parse**: <5ms for 400-line file
- **UI Build**: <50ms for 50-control window
- **Property Store R/W**: <1ms per operation
- **Script Execution Overhead**: ~10ms (process fork)

#### Scalability
- **Controls per Window**: Tested to 500 controls
- **Properties per Control**: Unlimited (reflection-based)
- **Concurrent Scripts**: Limited by OS (100+)
- **VML File Size**: No practical limit (text parsing)

### Security Model

#### File System
- VML files: Read-only by application
- Scripts: Execute with user permissions
- Database: /tmp/vb-runtime.db with 0600 permissions

#### Script Execution
**Current**: Full user privileges, no sandboxing  
**Planned**: 
- Capability-based permissions per @Script
- Whitelisted command execution
- Resource limits (CPU, memory, time)

**Critical**: Scripts have full system access. VML files from untrusted sources should be reviewed before loading.

### Error Handling

#### VML Parse Errors
```
[VML] Warning: Unknown property at line 42
[VML] Skipping malformed control definition
```

Parser continues, logging issues but not failing completely.

#### Script Errors
```
[SCRIPT] Exit code: 1
[SCRIPT] stderr: /bin/bash: line 5: syntax error
```

Script failures logged but don't crash the application.

#### Property Store Errors
```
[PROPERTY STORE] Warning: Database locked, retrying...
[PROPERTY STORE] Error: Property not found
```

Graceful degradation - missing properties don't crash UI.

## Future Enhancements

### 1. Complete VML Conversion
Convert remaining C# dialogs (MenuEditorWindow) to VML.

### 2. Self-Replication
```
VML in Database → Extract → Rebuild → Replace Binary → Restart
```

The IDE can rebuild itself from stored VML definitions.

### 3. Hot Reload
Edit VML files while running, reload windows instantly without restart.

### 4. Plugin System
```csharp
interface IVmlExtension
{
    Control? CreateControl(string type, Dictionary<string, string> props);
    void HandleEvent(string controlName, string eventName);
}
```

Extend VML with custom control types and behaviors.

### 5. VML Compilation
Pre-parse VML to binary format for faster loading.

### 6. Network Distribution
```bash
vb-fetch http://example.com/app.vml
./VB app.vml
```

Applications distributed as URLs, not binaries.

## Deployment

### Build Process
```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

### Dependencies
- Avalonia.Desktop 11.x
- Microsoft.Data.Sqlite 9.x
- .NET 9.0

### File Structure
```
VB/
├── VB                           # Main binary
├── *.vml                        # UI definitions
├── PropertyStore.db             # Runtime state (generated)
└── *.dll                        # Avalonia assemblies
```

### Distribution Model
**Current**: Traditional binary + VML files  
**Future**: Generic runtime + VML URLs

## Conclusion

Visualised Markup redefines RAD development:

- **Markup is code** - No generation step
- **Self-hosting** - IDE uses its own technology
- **Language-agnostic** - Scripts in any language
- **Human-readable** - VML is plain text
- **Zero-friction** - Edit and reload, no compile

The paradigm shift: applications as data, not executables.
