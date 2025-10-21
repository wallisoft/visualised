using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes; 
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ConfigUI.Designer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Path = System.IO.Path;  

namespace ConfigUI
{
    public class DesignerWindow : Window
    {
        private Canvas? _designCanvas;
        private TextBox? _yamlEditor;
        private TextBlock? _selectedControlLabel;
        private TextBlock? _propType;

        private Dictionary<string, TextBox?> _propertyEditors = new Dictionary<string, TextBox?>();
        private Dictionary<string, CheckBox?> _propertyCheckboxes = new Dictionary<string, CheckBox?>();
        private Dictionary<string, ComboBox?> _propertyComboBoxes = new Dictionary<string, ComboBox?>();
        
        private Panel? _toolboxPanel;
        private Panel? _propertiesPanel;
        private Panel? _editorPanel;
        private ScrollViewer? _canvasScroller;
        
        private List<DesignerControl> _designerControls = new List<DesignerControl>();
        private DesignerControl? _selectedControl = null;
        private Dictionary<string, int> _controlCounters = new Dictionary<string, int>();
        
        private DesignerControl? _draggedControl = null;
        private Point _dragStartPoint;
        private Point _dragStartControlPosition;

        private List<Rectangle> _resizeHandles = new List<Rectangle>();
        private Rectangle? _activeResizeHandle = null;
        private Point _resizeStartPoint;
        private Size _resizeStartSize;
        private Point _resizeStartPosition;
        private string _resizeDirection = "";
        
        private string? _draggingControlType = null;
        private Border? _ghostControl = null;
        private Point _ghostOffset; // NEW: Track offset for smoother drag
        
        private bool _updatingProperties = false;
        
        private ScriptDatabase? _scriptDatabase;
        private DesignerDatabase? _designerDatabase;
        private DesignerImportExport? _importExport;

        public DesignerWindow()
        {
            Width = 1400;
            Height = 900;
            Title = "Visualised Markup - Form Designer v1.0";
            Background = new SolidColorBrush(Color.Parse("#f0f0f0"));
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            _scriptDatabase = new ScriptDatabase();
            _designerDatabase = new DesignerDatabase(_scriptDatabase, _designerControls);
            _importExport = new DesignerImportExport(_designerControls, _scriptDatabase, this);
            
            InitializePanelDefinitions();
            ImportDesignerYamlToDatabase();

            this.Loaded += (s, e) => LoadDesignerUI();
            
            // Handle window resize for responsive panels - use SizeChanged instead
            this.SizeChanged += (s, e) =>
            {
                RepositionPanels();
            };
        }

        private void RepositionPanels()
        {
            if (_toolboxPanel != null)
            {
                Canvas.SetLeft(_toolboxPanel, 10);
                Canvas.SetTop(_toolboxPanel, 30);
                _toolboxPanel.Height = this.Height - 40; // Adjust height with window
            }
            
            if (_propertiesPanel != null)
            {
                Canvas.SetLeft(_propertiesPanel, this.Bounds.Width - 310);
                Canvas.SetTop(_propertiesPanel, 30);
                _propertiesPanel.Height = this.Height - 40; // Adjust height with window
            }
            
            if (_editorPanel != null)
            {
                // Reposition YAML editor panel to be centered between toolbox and properties
                var editorWidth = this.Bounds.Width - 620; // Total width minus both panels
                var editorX = 300; // After toolbox
                Canvas.SetLeft(_editorPanel, editorX);
                Canvas.SetTop(_editorPanel, this.Height - 210); // Bottom of window
                _editorPanel.Width = editorWidth;
            }
            
            // Also update the main canvas if needed
            if (this.Content is Canvas mainCanvas)
            {
                mainCanvas.Width = this.Bounds.Width;
                mainCanvas.Height = this.Bounds.Height;
            }
        }

        private void InitializePanelDefinitions()
        {
            if (_scriptDatabase == null) return;

            Console.WriteLine("🎨 Initializing panel definitions...");
            
            _scriptDatabase.ExecuteSql(@"
                CREATE TABLE IF NOT EXISTS panel_definitions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    panel_type TEXT NOT NULL,
                    panel_name TEXT NOT NULL,
                    x REAL,
                    y REAL,
                    width REAL,
                    height REAL,
                    background_color TEXT,
                    border_color TEXT,
                    visible INTEGER DEFAULT 1
                )
            ");
            
            _scriptDatabase.ExecuteSql(@"
                CREATE TABLE IF NOT EXISTS panel_controls (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    panel_id INTEGER,
                    control_type TEXT NOT NULL,
                    control_name TEXT NOT NULL,
                    label TEXT,
                    icon TEXT,
                    group_name TEXT,
                    x REAL,
                    y REAL,
                    width REAL,
                    height REAL,
                    display_order INTEGER,
                    action_type TEXT,
                    action_value TEXT,
                    FOREIGN KEY(panel_id) REFERENCES panel_definitions(id)
                )
            ");
            
            _scriptDatabase.ExecuteSql(@"
                CREATE TABLE IF NOT EXISTS property_definitions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    property_name TEXT NOT NULL,
                    property_label TEXT NOT NULL,
                    property_group TEXT NOT NULL,
                    editor_type TEXT NOT NULL,
                    display_order INTEGER,
                    default_value TEXT,
                    is_readonly INTEGER DEFAULT 0,
                    control_types TEXT,
                    group_color TEXT,
                    group_expanded INTEGER DEFAULT 1
                )
            ");
           
    _scriptDatabase.ExecuteSql(@"
        CREATE TABLE IF NOT EXISTS control_properties (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            control_id INTEGER NOT NULL,
            property_name TEXT NOT NULL,
            property_value TEXT,
            FOREIGN KEY(control_id) REFERENCES controls(id) ON DELETE CASCADE,
            UNIQUE(control_id, property_name)
        )
    ");
    
    Console.WriteLine("✅ control_properties table created!");
 
            var panelCount = _scriptDatabase.ExecuteScalarSql("SELECT COUNT(*) FROM panel_definitions");
            if (Convert.ToInt32(panelCount) == 0)
            {
                SeedPanelDefinitions();
            }
            
            Console.WriteLine("✅ Panel definitions initialized!");
        }

        private void SeedPanelDefinitions()
        {
            if (_scriptDatabase == null) return;
            
            Console.WriteLine("🌱 Seeding panel definitions...");
            
            _scriptDatabase.ExecuteSql(@"
                INSERT INTO panel_definitions (panel_type, panel_name, x, y, width, height, background_color, border_color)
                VALUES ('toolbox', 'ToolboxPanel', 10, 30, 280, 870, '#FFFFFF', '#c0c0c0')
            ");
            
            var toolboxId = _scriptDatabase.ExecuteScalarSql("SELECT last_insert_rowid()");
            
            var commonControls = new[]
            {
                ("button", "Label", "🏷️", "label", 1),
                ("button", "Button", "🔘", "button", 2),
                ("button", "TextBox", "📝", "textbox", 3),
                ("button", "CheckBox", "☑️", "checkbox", 4),
                ("button", "RadioButton", "⚪", "radiobutton", 5)
            };
            
            foreach (var (type, label, icon, value, order) in commonControls)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO panel_controls (panel_id, control_type, control_name, label, icon, group_name, display_order, action_type, action_value, width, height)
                    VALUES ({toolboxId}, '{type}', 'Add{label}', '{label}', '{icon}', 'Common Controls', {order}, 'create_control', '{value}', 250, 32)
                ");
            }
            
            var containerControls = new[]
            {
                ("button", "Panel", "📦", "panel", 10),
                ("button", "TabControl", "📑", "tabcontrol", 11)
            };
            
            foreach (var (type, label, icon, value, order) in containerControls)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO panel_controls (panel_id, control_type, control_name, label, icon, group_name, display_order, action_type, action_value, width, height)
                    VALUES ({toolboxId}, '{type}', 'Add{label}', '{label}', '{icon}', 'Containers', {order}, 'create_control', '{value}', 250, 32)
                ");
            }
            
            var dataControls = new[]
            {
                ("button", "ComboBox", "🔽", "combobox", 20),
                ("button", "ListBox", "📋", "listbox", 21),
                ("button", "Grid", "⊞", "grid", 22),
                ("button", "Data", "💾", "data", 23)
            };
            
            foreach (var (type, label, icon, value, order) in dataControls)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO panel_controls (panel_id, control_type, control_name, label, icon, group_name, display_order, action_type, action_value, width, height)
                    VALUES ({toolboxId}, '{type}', 'Add{label}', '{label}', '{icon}', 'Lists & Data', {order}, 'create_control', '{value}', 250, 32)
                ");
            }
            
            var menuControls = new[]
            {
                ("button", "MenuBar", "☰", "menubar", 30),
                ("button", "ToolBar", "🔧", "toolbar", 31)
            };
            
            foreach (var (type, label, icon, value, order) in menuControls)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO panel_controls (panel_id, control_type, control_name, label, icon, group_name, display_order, action_type, action_value, width, height)
                    VALUES ({toolboxId}, '{type}', 'Add{label}', '{label}', '{icon}', 'Menus & Toolbars', {order}, 'create_control', '{value}', 250, 32)
                ");
            }
            
            var otherControls = new[]
            {
                ("button", "ProgressBar", "▬", "progressbar", 40),
                ("button", "HScrollBar", "⟷", "hscrollbar", 41),
                ("button", "VScrollBar", "↕", "vscrollbar", 42),
                ("button", "Timer", "⏱", "timer", 43)
            };
            
            foreach (var (type, label, icon, value, order) in otherControls)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO panel_controls (panel_id, control_type, control_name, label, icon, group_name, display_order, action_type, action_value, width, height)
                    VALUES ({toolboxId}, '{type}', 'Add{label}', '{label}', '{icon}', 'Other', {order}, 'create_control', '{value}', 250, 32)
                ");
            }
            
            _scriptDatabase.ExecuteSql(@"
                INSERT INTO panel_definitions (panel_type, panel_name, x, y, width, height, background_color, border_color)
                VALUES ('properties', 'PropertiesPanel', 1100, 30, 300, 870, '#FFFFFF', '#c0c0c0')
            ");
            
            var identityProps = new[]
            {
                ("type", "Type", "textbox", 1, "", 1, "#1976D2"),
                ("name", "Name", "textbox", 2, "", 0, "#1976D2"),
                ("tag", "Tag", "textbox", 3, "", 0, "#1976D2"),
                ("index", "Index", "textbox", 4, "0", 0, "#1976D2")
            };
            
            foreach (var (name, label, editor, order, defValue, isReadonly, color) in identityProps)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO property_definitions (property_name, property_label, property_group, editor_type, display_order, default_value, is_readonly, group_color, group_expanded)
                    VALUES ('{name}', '{label}', 'Identity', '{editor}', {order}, '{defValue}', {isReadonly}, '{color}', 1)
                ");
            }
            
            var positionProps = new[]
            {
                ("x", "X", "textbox", 10, "0", 0, "#388E3C"),
                ("y", "Y", "textbox", 11, "0", 0, "#388E3C"),
                ("z_index", "Z-Index", "textbox", 12, "0", 0, "#388E3C")
            };
            
            foreach (var (name, label, editor, order, defValue, isReadonly, color) in positionProps)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO property_definitions (property_name, property_label, property_group, editor_type, display_order, default_value, is_readonly, group_color, group_expanded)
                    VALUES ('{name}', '{label}', 'Position', '{editor}', {order}, '{defValue}', {isReadonly}, '{color}', 1)
                ");
            }
            
            var sizeProps = new[]
            {
                ("width", "Width", "textbox", 20, "100", 0, "#F57C00"),
                ("height", "Height", "textbox", 21, "30", 0, "#F57C00")
            };
            
            foreach (var (name, label, editor, order, defValue, isReadonly, color) in sizeProps)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO property_definitions (property_name, property_label, property_group, editor_type, display_order, default_value, is_readonly, group_color, group_expanded)
                    VALUES ('{name}', '{label}', 'Size', '{editor}', {order}, '{defValue}', {isReadonly}, '{color}', 1)
                ");
            }
            
            var contentProps = new[]
            {
                ("caption", "Caption", "textbox", 30, "", 0, "#7B1FA2"),
                ("text", "Text", "textbox", 31, "", 0, "#7B1FA2")
            };
            
            foreach (var (name, label, editor, order, defValue, isReadonly, color) in contentProps)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO property_definitions (property_name, property_label, property_group, editor_type, display_order, default_value, is_readonly, group_color, group_expanded)
                    VALUES ('{name}', '{label}', 'Content', '{editor}', {order}, '{defValue}', {isReadonly}, '{color}', 1)
                ");
            }
            
            var appearanceProps = new[]
            {
                ("background_color", "BackColor", "textbox", 40, "", 0, "#C62828"),
                ("foreground_color", "ForeColor", "textbox", 41, "", 0, "#C62828"),
                ("border_style", "BorderStyle", "combobox", 42, "None", 0, "#C62828"),
                ("opacity", "Opacity", "textbox", 43, "1.0", 0, "#C62828")
            };
            
            foreach (var (name, label, editor, order, defValue, isReadonly, color) in appearanceProps)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO property_definitions (property_name, property_label, property_group, editor_type, display_order, default_value, is_readonly, group_color, group_expanded)
                    VALUES ('{name}', '{label}', 'Appearance', '{editor}', {order}, '{defValue}', {isReadonly}, '{color}', 1)
                ");
            }
            
            var fontProps = new[]
            {
                ("font_family", "Name", "textbox", 50, "Segoe UI", 0, "#00796B"),
                ("font_size", "Size", "textbox", 51, "12", 0, "#00796B"),
                ("font_bold", "Bold", "checkbox", 52, "false", 0, "#00796B"),
                ("font_italic", "Italic", "checkbox", 53, "false", 0, "#00796B"),
                ("font_underline", "Underline", "checkbox", 54, "false", 0, "#00796B")
            };
            
            foreach (var (name, label, editor, order, defValue, isReadonly, color) in fontProps)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO property_definitions (property_name, property_label, property_group, editor_type, display_order, default_value, is_readonly, group_color, group_expanded)
                    VALUES ('{name}', '{label}', 'Font', '{editor}', {order}, '{defValue}', {isReadonly}, '{color}', 1)
                ");
            }
            
            var behaviorProps = new[]
            {
                ("visible", "Visible", "checkbox", 60, "true", 0, "#5D4037"),
                ("enabled", "Enabled", "checkbox", 61, "true", 0, "#5D4037"),
                ("tab_stop", "TabStop", "checkbox", 62, "true", 0, "#5D4037"),
                ("tab_index", "TabIndex", "textbox", 63, "0", 0, "#5D4037")
            };
            
            foreach (var (name, label, editor, order, defValue, isReadonly, color) in behaviorProps)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO property_definitions (property_name, property_label, property_group, editor_type, display_order, default_value, is_readonly, group_color, group_expanded)
                    VALUES ('{name}', '{label}', 'Behavior', '{editor}', {order}, '{defValue}', {isReadonly}, '{color}', 1)
                ");
            }
            
            var layoutProps = new[]
            {
                ("margin", "Margin", "textbox", 70, "0", 0, "#00897B"),
                ("padding", "Padding", "textbox", 71, "0", 0, "#00897B"),
                ("horizontal_alignment", "HAlign", "combobox", 72, "Left", 0, "#00897B"),
                ("vertical_alignment", "VAlign", "combobox", 73, "Top", 0, "#00897B")
            };
            
            foreach (var (name, label, editor, order, defValue, isReadonly, color) in layoutProps)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO property_definitions (property_name, property_label, property_group, editor_type, display_order, default_value, is_readonly, group_color, group_expanded)
                    VALUES ('{name}', '{label}', 'Layout', '{editor}', {order}, '{defValue}', {isReadonly}, '{color}', 0)
                ");
            }
            
            var dataProps = new[]
            {
                ("data_source", "DataSource", "textbox", 80, "", 0, "#6A1B9A"),
                ("data_member", "DataMember", "textbox", 81, "", 0, "#6A1B9A"),
                ("data_field", "DataField", "textbox", 82, "", 0, "#6A1B9A")
            };
            
            foreach (var (name, label, editor, order, defValue, isReadonly, color) in dataProps)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO property_definitions (property_name, property_label, property_group, editor_type, display_order, default_value, is_readonly, group_color, group_expanded)
                    VALUES ('{name}', '{label}', 'Data Binding', '{editor}', {order}, '{defValue}', {isReadonly}, '{color}', 0)
                ");
            }
            
            var advancedProps = new[]
            {
                ("tooltip", "ToolTip", "textbox", 90, "", 0, "#424242"),
                ("cursor", "Cursor", "combobox", 91, "Default", 0, "#424242"),
                ("anchor", "Anchor", "textbox", 92, "None", 0, "#424242"),
                ("dock", "Dock", "combobox", 93, "None", 0, "#424242")
            };
            
            foreach (var (name, label, editor, order, defValue, isReadonly, color) in advancedProps)
            {
                _scriptDatabase.ExecuteSql($@"
                    INSERT INTO property_definitions (property_name, property_label, property_group, editor_type, display_order, default_value, is_readonly, group_color, group_expanded)
                    VALUES ('{name}', '{label}', 'Advanced', '{editor}', {order}, '{defValue}', {isReadonly}, '{color}', 0)
                ");
            }
            
            Console.WriteLine("✅ Panel definitions seeded!");
        }

        private void ImportDesignerYamlToDatabase()
        {
            // No longer needed - designer is 100% database-driven now
            Console.WriteLine("✅ Designer is 100% database-driven!");
        }

        private void LoadDesignerUI()
        {
            try
            {
                Console.WriteLine("🎨 Building designer UI from database...");
                
                var mainCanvas = new Canvas
                {
                    Width = this.Width,
                    Height = this.Height,
                    Background = new SolidColorBrush(Color.Parse("#f0f0f0"))
                };
                
                this.Content = mainCanvas;
                
                // Build main menu from scratch
                var mainMenu = CreateMainMenu();
                mainCanvas.Children.Add(mainMenu);
                
                // Build design canvas
                _designCanvas = new Canvas
                {
                    Width = 800,
                    Height = 600,
                    Background = new SolidColorBrush(Colors.White),
                    ClipToBounds = true
                };
                Canvas.SetLeft(_designCanvas, 300);
                Canvas.SetTop(_designCanvas, 60);
                
                var canvasBorder = new Border
                {
                    Width = 800,
                    Height = 600,
                    BorderBrush = new SolidColorBrush(Color.Parse("#BDBDBD")),
                    BorderThickness = new Thickness(2),
                    Child = _designCanvas
                };
                Canvas.SetLeft(canvasBorder, 300);
                Canvas.SetTop(canvasBorder, 60);
                mainCanvas.Children.Add(canvasBorder);
                
                // Build YAML editor panel
                _editorPanel = CreateYamlEditorPanel();
                mainCanvas.Children.Add(_editorPanel);
                
                // Build database-driven panels
                Console.WriteLine("📦 Building panels from database...");
                var newToolbox = BuildPanelFromDatabase("toolbox");
                if (newToolbox != null)
                {
                    mainCanvas.Children.Add(newToolbox);
                    _toolboxPanel = newToolbox;
                }
                
                var newPropsPanel = BuildPanelFromDatabase("properties");
                if (newPropsPanel != null)
                {
                    mainCanvas.Children.Add(newPropsPanel);
                    _propertiesPanel = newPropsPanel;
                }
                
                SetupCanvasEvents(_designCanvas);
                
                Console.WriteLine("✅ Designer UI loaded from database!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to load designer UI: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private Menu CreateMainMenu()
        {
            var menu = new Menu
            {
                Height = 30,
                Background = new SolidColorBrush(Color.Parse("#f5f5f5"))
            };
            Canvas.SetLeft(menu, 0);
            Canvas.SetTop(menu, 0);
            Canvas.SetRight(menu, 0);
            
            // File menu
            var fileMenu = new MenuItem { Header = "File" };
            fileMenu.Items.Add(new MenuItem { Header = "New Form" });
            fileMenu.Items.Add(new MenuItem { Header = "Open Form..." });
            fileMenu.Items.Add(new MenuItem { Header = "Save Form..." });
            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(new MenuItem { Header = "Save to Database" });
            fileMenu.Items.Add(new MenuItem { Header = "Load from Database" });
            fileMenu.Items.Add(new MenuItem { Header = "Export YAML from Database" });
            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(new MenuItem { Header = "Reset Database" });
            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(new MenuItem { Header = "Exit" });
            menu.Items.Add(fileMenu);
            
            // View menu
            var viewMenu = new MenuItem { Header = "View" };
            viewMenu.Items.Add(new MenuItem { Header = "Toolbox" });
            viewMenu.Items.Add(new MenuItem { Header = "Properties" });
            viewMenu.Items.Add(new MenuItem { Header = "YAML Editor" });
            menu.Items.Add(viewMenu);
            
            // Help menu
            var helpMenu = new MenuItem { Header = "Help" };
            helpMenu.Items.Add(new MenuItem { Header = "About" });
            menu.Items.Add(helpMenu);
            
            // Wire up events
            HookupMenuEvents(menu);
            SetupCanvasSizeMenu(menu);
            
            return menu;
        }

        private void HookupMenuEvents(Menu menu)
        {
            foreach (var topItem in menu.Items.Cast<MenuItem>())
            {
                if (topItem.Header?.ToString() == "File")
                {
                    foreach (var fileItem in topItem.Items.Cast<object>())
                    {
                        if (fileItem is MenuItem mi)
                        {
                            var header = mi.Header?.ToString();
                            if (header == "Open Form...")
                                mi.Click += async (s, e) => await _importExport?.ShowImportDialog()!;
                            else if (header == "Save Form...")
                                mi.Click += async (s, e) => await _importExport?.ShowExportDialog()!;
                            else if (header == "Save to Database")
                                mi.Click += (s, e) => _designerDatabase?.SaveAllToDatabase();
                            else if (header == "Load from Database")
                                mi.Click += (s, e) => LoadAllFromDatabase();
                            else if (header == "Export YAML from Database")
                                mi.Click += async (s, e) => { LoadAllFromDatabase(); await _importExport?.ShowExportDialog()!; };
                            else if (header == "Reset Database")
                                mi.Click += (s, e) => ResetDatabase();
                            else if (header == "Exit")
                                mi.Click += (s, e) => this.Close();
                        }
                    }
                }
                else if (topItem.Header?.ToString() == "View")
                {
                    foreach (var viewItem in topItem.Items.Cast<object>())
                    {
                        if (viewItem is MenuItem mi)
                        {
                            var header = mi.Header?.ToString();
                            if (header == "Toolbox")
                                mi.Click += (s, e) => { if (_toolboxPanel != null) _toolboxPanel.IsVisible = !_toolboxPanel.IsVisible; };
                            else if (header == "Properties")
                                mi.Click += (s, e) => { if (_propertiesPanel != null) _propertiesPanel.IsVisible = !_propertiesPanel.IsVisible; };
                            else if (header == "YAML Editor")
                                mi.Click += (s, e) => { if (_editorPanel != null) _editorPanel.IsVisible = !_editorPanel.IsVisible; };
                        }
                    }
                }
                else if (topItem.Header?.ToString() == "Help")
                {
                    foreach (var helpItem in topItem.Items.Cast<object>())
                    {
                        if (helpItem is MenuItem mi && mi.Header?.ToString() == "About")
                        {
                            mi.Click += (s, e) => ShowAbout();
                        }
                    }
                }
            }
        }

        private Panel CreateYamlEditorPanel()
        {
            var panel = new Panel
            {
                Width = 780,
                Height = 200,
                Background = new SolidColorBrush(Colors.White)
            };
            Canvas.SetLeft(panel, 300);
            Canvas.SetTop(panel, 680);
            
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#c0c0c0")),
                BorderThickness = new Thickness(1)
            };
            
            var stack = new StackPanel();
            
            var header = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#e8eaf6")),
                Padding = new Thickness(10, 5, 10, 5),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "📝 YAML OUTPUT",
                            FontSize = 12,
                            FontWeight = FontWeight.Bold,
                            Foreground = new SolidColorBrush(Color.Parse("#3F51B5")),
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new Button
                        {
                            Content = "▶ Preview",
                            Margin = new Thickness(10, 0, 0, 0),
                            Padding = new Thickness(8, 4, 8, 4),
                            Background = new SolidColorBrush(Color.Parse("#4CAF50")),
                            Foreground = new SolidColorBrush(Colors.White),
                            BorderThickness = new Thickness(0),
                            Cursor = new Cursor(StandardCursorType.Hand)
                        }
                    }
                }
            };
            stack.Children.Add(header);
            
            _yamlEditor = new TextBox
            {
                Width = 778,
                Height = 155,
                FontFamily = new FontFamily("Consolas,Monaco,monospace"),
                FontSize = 11,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                Background = new SolidColorBrush(Color.Parse("#FAFAFA")),
                IsReadOnly = true
            };
            
            var scrollViewer = new ScrollViewer
            {
                Content = _yamlEditor,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            };
            stack.Children.Add(scrollViewer);
            
            border.Child = stack;
            panel.Children.Add(border);
            
            // Wire up preview button
            var previewBtn = ((header.Child as StackPanel)?.Children[1] as Button);
            if (previewBtn != null)
            {
                previewBtn.Click += (s, e) => LaunchPreview();
            }
            
            return panel;
        }

	private Panel? BuildPanelFromDatabase(string panelType)
        {
            if (_scriptDatabase == null) return null;

            try
            {
                var panelInfo = GetPanelDefinition(panelType);
                if (panelInfo == null)
                {
                    Console.WriteLine($"❌ No panel definition found for: {panelType}");
                    return null;
                }

                Console.WriteLine($"📦 Building {panelType} panel from database...");

                var mainPanel = new Panel
                {
                    Width = panelInfo.Width,
                    Height = panelInfo.Height,
                    Background = new SolidColorBrush(Color.Parse(panelInfo.BackgroundColor))
                };
                
                if (panelType == "toolbox")
                {
                    Canvas.SetLeft(mainPanel, 10);
                    Canvas.SetTop(mainPanel, 30);
                }
                else if (panelType == "properties")
                {
                    Canvas.SetLeft(mainPanel, this.Width - panelInfo.Width - 10);
                    Canvas.SetTop(mainPanel, 30);
                }
                else
                {
                    Canvas.SetLeft(mainPanel, panelInfo.X);
                    Canvas.SetTop(mainPanel, panelInfo.Y);
                }
                
                var border = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.Parse(panelInfo.BorderColor)),
                    BorderThickness = new Thickness(1),
                    Child = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                        Content = panelType == "toolbox" ? BuildToolboxContent(panelInfo.Id) : BuildPropertiesContent(panelInfo.Id)
                    }
                };
                
                mainPanel.Children.Add(border);
                Console.WriteLine($"✅ Built {panelType} panel from database!");
                return mainPanel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error building {panelType} panel: {ex.Message}");
                return null;
            }
        }

        private PanelDefinition? GetPanelDefinition(string panelType)
        {
            if (_scriptDatabase == null) return null;

            var sql = $@"SELECT id, x, y, width, height, background_color, border_color 
                        FROM panel_definitions 
                        WHERE panel_type = '{panelType}' 
                        LIMIT 1";
            
            try
            {
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                    $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/VisualisedDesigner/designer.db");
                connection.Open();
                
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new PanelDefinition
                    {
                        Id = reader.GetInt32(0),
                        X = reader.GetDouble(1),
                        Y = reader.GetDouble(2),
                        Width = reader.GetDouble(3),
                        Height = reader.GetDouble(4),
                        BackgroundColor = reader.GetString(5),
                        BorderColor = reader.GetString(6)
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading panel definition: {ex.Message}");
            }
            
            return null;
        }

        private StackPanel BuildToolboxContent(int panelId)
        {
            var stack = new StackPanel { Margin = new Thickness(10) };
            
            var header = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#e3f2fd")),
                Padding = new Thickness(10),
                Margin = new Thickness(-10, -10, -10, 10),
                Child = new TextBlock
                {
                    Text = "🧰 TOOLBOX",
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#1976D2")),
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };
            stack.Children.Add(header);

            var controls = GetPanelControls(panelId);
            var groupedControls = controls.GroupBy(c => c.GroupName).OrderBy(g => g.First().DisplayOrder);

            foreach (var group in groupedControls)
            {
                var groupColor = GetGroupColor(group.Key);
                var expander = CreateToolboxGroup($"▼ {group.Key}", groupColor);
                var groupStack = new StackPanel { Margin = new Thickness(5) };
                
                foreach (var control in group.OrderBy(c => c.DisplayOrder))
                {
                    AddDatabaseToolboxButton(groupStack, control);
                }
                
                expander.Content = groupStack;
                expander.IsExpanded = group.First().DisplayOrder < 20;
                stack.Children.Add(expander);
            }

            return stack;
        }

        private void AddDatabaseToolboxButton(StackPanel parent, PanelControlDefinition control)
        {
            var button = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock { Text = control.Icon, FontSize = 14, Margin = new Thickness(0, 0, 8, 0), Width = 20 },
                        new TextBlock { Text = control.Label, FontSize = 11, VerticalAlignment = VerticalAlignment.Center }
                    }
                },
                Width = control.Width,
                Height = control.Height,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 4),
                Padding = new Thickness(8, 6, 8, 6),
                Background = new SolidColorBrush(Color.Parse("#fafafa")),
                BorderBrush = new SolidColorBrush(Color.Parse("#e0e0e0")),
                BorderThickness = new Thickness(1),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            
            var controlType = control.ActionValue;
            button.AddHandler(PointerPressedEvent, (EventHandler<PointerPressedEventArgs>)((s, e) =>
            {
                if (e.GetCurrentPoint(button).Properties.IsLeftButtonPressed)
                {
                    _draggingControlType = controlType;
                    
                    var pointerPosInButton = e.GetPosition(button);
                    _ghostOffset = new Point(pointerPosInButton.X, pointerPosInButton.Y);
                    
                    CreateGhostControl(controlType);
                    
                    if (_ghostControl != null && _designCanvas != null)
                    {
                        var pointerPosInCanvas = e.GetPosition(_designCanvas);
                        Canvas.SetLeft(_ghostControl, pointerPosInCanvas.X - _ghostOffset.X);
                        Canvas.SetTop(_ghostControl, pointerPosInCanvas.Y - _ghostOffset.Y);
                    }
                    
                    Console.WriteLine($"👻 Started dragging {controlType}");
                }
            }), RoutingStrategies.Tunnel);
            
            parent.Children.Add(button);
        }

        private StackPanel BuildPropertiesContent(int panelId)
        {
            var stack = new StackPanel { Margin = new Thickness(10) };
            
            var header = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#e8f5e9")),
                Padding = new Thickness(10),
                Margin = new Thickness(-10, -10, -10, 10),
                Child = new TextBlock
                {
                    Text = "⚙️ PROPERTIES",
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#2E7D32")),
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };
            stack.Children.Add(header);
            
            _selectedControlLabel = new TextBlock
            {
                Text = "No control selected",
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#1565C0")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(_selectedControlLabel);

            var properties = GetPropertyDefinitions();
            var groupedProps = properties.GroupBy(p => p.PropertyGroup).OrderBy(g => g.First().DisplayOrder);

            foreach (var group in groupedProps)
            {
                var firstProp = group.First();
                var groupColor = Color.Parse(firstProp.GroupColor);
                var expander = CreatePropertyGroup($"▼ {group.Key}", groupColor);
                var groupStack = new StackPanel { Margin = new Thickness(10, 5, 0, 5) };
                
                foreach (var prop in group.OrderBy(p => p.DisplayOrder))
                {
                    AddPropertyEditor(groupStack, prop);
                }
                
                expander.Content = groupStack;
                expander.IsExpanded = firstProp.GroupExpanded;
                stack.Children.Add(expander);
            }

            return stack;
        }

	  private void AddPropertyEditor(StackPanel parent, PropertyDefinition prop)
        {
            if (prop.EditorType == "textbox")
            {
                var grid = new Grid
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    ColumnDefinitions = new ColumnDefinitions("70,*")
                };
                
                var label = new TextBlock
                {
                    Text = $"{prop.PropertyLabel}:",
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);
                
                if (prop.PropertyName == "type")
                {
                    _propType = new TextBlock
                    {
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.Parse("#666666")),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(_propType, 1);
                    grid.Children.Add(_propType);
                }
                else
                {
                    var textBox = new TextBox
                    {
                        Width = 180,
                        Height = 25,
                        FontSize = 11,
                        IsReadOnly = prop.IsReadonly,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    
                    if (prop.IsReadonly)
                    {
                        textBox.Foreground = new SolidColorBrush(Color.Parse("#666666"));
                        textBox.Background = new SolidColorBrush(Color.Parse("#f5f5f5"));
                    }
                    else
                    {
                        var propName = prop.PropertyName;
                        
                        textBox.LostFocus += (s, e) =>
                        {
                            if (_selectedControl?.DatabaseId != null)
                            {
                                SaveControlProperty(_selectedControl.DatabaseId.Value, propName, textBox.Text ?? "");
                                UpdateControlFromProperty(propName, textBox.Text);
                            }
                        };
                        
                        textBox.KeyDown += (s, e) =>
                        {
                            if (e.Key == Key.Enter)
                            {
                                if (_selectedControl?.DatabaseId != null)
                                {
                                    SaveControlProperty(_selectedControl.DatabaseId.Value, propName, textBox.Text ?? "");
                                    UpdateControlFromProperty(propName, textBox.Text);
                                }
                                e.Handled = true;
                            }
                        };
                    }
                    
                    _propertyEditors[prop.PropertyName] = textBox;
                    Grid.SetColumn(textBox, 1);
                    grid.Children.Add(textBox);
                }
                
                parent.Children.Add(grid);
            }
            else if (prop.EditorType == "checkbox")
            {
                var checkBox = new CheckBox
                {
                    Content = prop.PropertyLabel,
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                
                var propName = prop.PropertyName;
                
                checkBox.Click += (s, e) =>
                {
                    if (_selectedControl?.DatabaseId != null)
                    {
                        SaveControlProperty(_selectedControl.DatabaseId.Value, propName, checkBox.IsChecked?.ToString() ?? "false");
                    }
                };
                
                _propertyCheckboxes[prop.PropertyName] = checkBox;
                parent.Children.Add(checkBox);
            }
        }

        private List<PanelControlDefinition> GetPanelControls(int panelId)
        {
            var controls = new List<PanelControlDefinition>();
            if (_scriptDatabase == null) return controls;

            try
            {
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                    $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/VisualisedDesigner/designer.db");
                connection.Open();
                
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    SELECT control_name, label, icon, group_name, display_order, action_type, action_value, width, height
                    FROM panel_controls 
                    WHERE panel_id = {panelId}
                    ORDER BY display_order";
                
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    controls.Add(new PanelControlDefinition
                    {
                        ControlName = reader.GetString(0),
                        Label = reader.GetString(1),
                        Icon = reader.GetString(2),
                        GroupName = reader.GetString(3),
                        DisplayOrder = reader.GetInt32(4),
                        ActionType = reader.GetString(5),
                        ActionValue = reader.GetString(6),
                        Width = reader.GetDouble(7),
                        Height = reader.GetDouble(8)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading panel controls: {ex.Message}");
            }

            return controls;
        }

        private List<PropertyDefinition> GetPropertyDefinitions()
        {
            var properties = new List<PropertyDefinition>();
            if (_scriptDatabase == null) return properties;

            try
            {
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                    $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/VisualisedDesigner/designer.db");
                connection.Open();
                
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT property_name, property_label, property_group, editor_type, 
                           display_order, default_value, is_readonly, group_color, group_expanded
                    FROM property_definitions 
                    ORDER BY display_order";
                
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    properties.Add(new PropertyDefinition
                    {
                        PropertyName = reader.GetString(0),
                        PropertyLabel = reader.GetString(1),
                        PropertyGroup = reader.GetString(2),
                        EditorType = reader.GetString(3),
                        DisplayOrder = reader.GetInt32(4),
                        DefaultValue = reader.GetString(5),
                        IsReadonly = reader.GetInt32(6) == 1,
                        GroupColor = reader.GetString(7),
                        GroupExpanded = reader.GetInt32(8) == 1
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading property definitions: {ex.Message}");
            }

            return properties;
        }

        private Dictionary<string, string> LoadControlProperties(int controlId)
        {
            var properties = new Dictionary<string, string>();
            if (_scriptDatabase == null) return properties;

            try
            {
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                    $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/VisualisedDesigner/designer.db");
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    SELECT property_name, property_value
                    FROM control_properties
                    WHERE control_id = {controlId}";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    properties[reader.GetString(0)] = reader.IsDBNull(1) ? "" : reader.GetString(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading control properties: {ex.Message}");
            }

            return properties;
        }

        private void SaveControlProperty(int controlId, string propertyName, string propertyValue)
        {
            if (_scriptDatabase == null) return;

            try
            {
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                    $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/VisualisedDesigner/designer.db");
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    INSERT INTO control_properties (control_id, property_name, property_value)
                    VALUES ({controlId}, '{propertyName.Replace("'", "''")}', '{propertyValue.Replace("'", "''")}')
                    ON CONFLICT(control_id, property_name)
                    DO UPDATE SET property_value = '{propertyValue.Replace("'", "''")}'";

                cmd.ExecuteNonQuery();
                Console.WriteLine($"💾 Saved property: {propertyName} = {propertyValue}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error saving property: {ex.Message}");
            }
        }

        private string GetPropertyValue(Dictionary<string, string> controlProps, string propertyName, string defaultValue)
        {
            return controlProps.ContainsKey(propertyName) ? controlProps[propertyName] : defaultValue;
        }

        private Color GetGroupColor(string groupName)
        {
            return groupName switch
            {
                "Common Controls" => Color.Parse("#1976D2"),
                "Containers" => Color.Parse("#388E3C"),
                "Lists & Data" => Color.Parse("#F57C00"),
                "Menus & Toolbars" => Color.Parse("#7B1FA2"),
                "Other" => Color.Parse("#5D4037"),
                _ => Color.Parse("#757575")
            };
        }

        private class PanelDefinition
        {
            public int Id { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public string BackgroundColor { get; set; } = "#FFFFFF";
            public string BorderColor { get; set; } = "#c0c0c0";
        }

        private class PanelControlDefinition
        {
            public string ControlName { get; set; } = "";
            public string Label { get; set; } = "";
            public string Icon { get; set; } = "";
            public string GroupName { get; set; } = "";
            public int DisplayOrder { get; set; }
            public string ActionType { get; set; } = "";
            public string ActionValue { get; set; } = "";
            public double Width { get; set; }
            public double Height { get; set; }
        }

        private class PropertyDefinition
        {
            public string PropertyName { get; set; } = "";
            public string PropertyLabel { get; set; } = "";
            public string PropertyGroup { get; set; } = "";
            public string EditorType { get; set; } = "";
            public int DisplayOrder { get; set; }
            public string DefaultValue { get; set; } = "";
            public bool IsReadonly { get; set; }
            public string GroupColor { get; set; } = "#000000";
            public bool GroupExpanded { get; set; }
        }

// ... existing SetupCanvasSizeMenu method above ...
        
        // NEW RESIZE METHODS START HERE ⬇️⬇️⬇️
        
        private void CreateResizeHandles(Border controlBorder, DesignerControl control)
        {
            if (_designCanvas == null) return;
            
            // Remove any existing handles first
            RemoveResizeHandles();
            
            var handleSize = 8.0;
            var handleColor = Color.Parse("#2196F3");
            var handleBorder = Colors.White;
            
            // 8 handle positions: NW, N, NE, E, SE, S, SW, W
            var positions = new[]
            {
                ("nw", 0.0, 0.0),
                ("n", 0.5, 0.0),
                ("ne", 1.0, 0.0),
                ("e", 1.0, 0.5),
                ("se", 1.0, 1.0),
                ("s", 0.5, 1.0),
                ("sw", 0.0, 1.0),
                ("w", 0.0, 0.5)
            };
            
            foreach (var (direction, xRatio, yRatio) in positions)
            {
                var handle = new Rectangle
                {
                    Width = handleSize,
                    Height = handleSize,
                    Fill = new SolidColorBrush(handleColor),
                    Stroke = new SolidColorBrush(handleBorder),
                    StrokeThickness = 1,
                    Cursor = GetResizeCursor(direction),
                    Tag = direction,
                    ZIndex = 1000
                };
                
                var x = control.X + (control.Width * xRatio) - (handleSize / 2);
                var y = control.Y + (control.Height * yRatio) - (handleSize / 2);
                
                Canvas.SetLeft(handle, x);
                Canvas.SetTop(handle, y);
                
                // Handle events
                handle.PointerPressed += (s, e) =>
                {
                    _activeResizeHandle = handle;
                    _resizeDirection = handle.Tag?.ToString() ?? "";
                    _resizeStartPoint = e.GetPosition(_designCanvas);
                    _resizeStartSize = new Size(control.Width, control.Height);
                    _resizeStartPosition = new Point(control.X, control.Y);
                    e.Handled = true;
                    Console.WriteLine($"🎯 Started resizing: {_resizeDirection}");
                };
                
                _resizeHandles.Add(handle);
                _designCanvas.Children.Add(handle);
            }
        }

        private void RemoveResizeHandles()
        {
            if (_designCanvas == null) return;
            
            foreach (var handle in _resizeHandles)
            {
                _designCanvas.Children.Remove(handle);
            }
            _resizeHandles.Clear();
        }

        private Cursor GetResizeCursor(string direction)
        {
            return direction switch
            {
                "nw" or "se" => new Cursor(StandardCursorType.TopLeftCorner),
                "ne" or "sw" => new Cursor(StandardCursorType.TopRightCorner),
                "n" or "s" => new Cursor(StandardCursorType.SizeNorthSouth),
                "e" or "w" => new Cursor(StandardCursorType.SizeWestEast),
                _ => new Cursor(StandardCursorType.Arrow)
            };
        }

        private void UpdateResizeHandles(DesignerControl control)
        {
            if (_resizeHandles.Count == 0) return;
            
            var handleSize = 8.0;
            var positions = new[]
            {
                (0.0, 0.0),  // nw
                (0.5, 0.0),  // n
                (1.0, 0.0),  // ne
                (1.0, 0.5),  // e
                (1.0, 1.0),  // se
                (0.5, 1.0),  // s
                (0.0, 1.0),  // sw
                (0.0, 0.5)   // w
            };
            
            for (int i = 0; i < _resizeHandles.Count && i < positions.Length; i++)
            {
                var (xRatio, yRatio) = positions[i];
                var x = control.X + (control.Width * xRatio) - (handleSize / 2);
                var y = control.Y + (control.Height * yRatio) - (handleSize / 2);
                
                Canvas.SetLeft(_resizeHandles[i], x);
                Canvas.SetTop(_resizeHandles[i], y);
            }
        }

	private void ShowAbout()
        {
            var aboutWindow = new Window
            {
                Title = "About Visualised",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };
            
            var stack = new StackPanel
            {
                Margin = new Thickness(20)
            };
            
            stack.Children.Add(new TextBlock
            {
                Text = "Visualised",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });
            
            stack.Children.Add(new TextBlock
            {
                Text = "Language-Agnostic RAD IDE",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#666")),
                Margin = new Thickness(0, 0, 0, 20)
            });
            
            stack.Children.Add(new TextBlock
            {
                Text = "Version 1.0 - Database-Driven Designer",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 20)
            });
            
            stack.Children.Add(new TextBlock
            {
                Text = "Built with recursive self-design principles.\nThe designer designs itself from SQLite.",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            });
            
            var okButton = new Button
            {
                Content = "OK",
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            okButton.Click += (s, e) => aboutWindow.Close();
            stack.Children.Add(okButton);
            
            aboutWindow.Content = stack;
            aboutWindow.ShowDialog(this);
        }

        private void UpdateControlFromProperty(string property, string? value)
        {
            if (_updatingProperties || _selectedControl == null || _designCanvas == null) return;
            
            try
            {
                switch (property)
                {
                    case "name":
                        _selectedControl.Name = value ?? "";
                        RefreshVisualControl(_selectedControl);
                        break;
                    case "x":
                        if (double.TryParse(value, out double x))
                        {
                            _selectedControl.X = x;
                            var border = _designCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == _selectedControl);
                            if (border != null) Canvas.SetLeft(border, x);
                        }
                        break;
                    case "y":
                        if (double.TryParse(value, out double y))
                        {
                            _selectedControl.Y = y;
                            var border = _designCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == _selectedControl);
                            if (border != null) Canvas.SetTop(border, y);
                        }
                        break;
                    case "width":
                        if (double.TryParse(value, out double width))
                        {
                            _selectedControl.Width = width;
                            RefreshVisualControl(_selectedControl);
                        }
                        break;
                    case "height":
                        if (double.TryParse(value, out double height))
                        {
                            _selectedControl.Height = height;
                            RefreshVisualControl(_selectedControl);
                        }
                        break;
                    case "caption":
                        _selectedControl.Caption = value;
                        RefreshVisualControl(_selectedControl);
                        break;
                    case "text":
                        _selectedControl.Text = value;
                        RefreshVisualControl(_selectedControl);
                        break;
                }
                
                if (_designerDatabase != null && _selectedControl != null)
                {
                    _designerDatabase.SaveControl(_selectedControl);
                }
                
                UpdateYamlEditor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error: {ex.Message}");
            }
        }

        private void RefreshVisualControl(DesignerControl control)
        {
            if (_designCanvas == null) return;
            
            var border = _designCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == control);
            if (border == null) return;
            
            var isSelected = border.BorderBrush is SolidColorBrush brush && brush.Color == Color.Parse("#2196F3");
            
            _designCanvas.Children.Remove(border);
            
            var newBorder = CreateVisualControl(control);
            if (isSelected)
            {
                newBorder.BorderBrush = new SolidColorBrush(Color.Parse("#2196F3"));
                newBorder.BorderThickness = new Thickness(2);
            }
            
            _designCanvas.Children.Add(newBorder);
        }

        private void SetupCanvasEvents(Canvas canvas)
        {
            this.AddHandler(PointerMovedEvent, (EventHandler<PointerEventArgs>)((s, e) =>
            {
                if (_draggingControlType != null && _ghostControl != null && _designCanvas != null)
                {
                    var pos = e.GetPosition(_designCanvas);
                    Canvas.SetLeft(_ghostControl, pos.X - _ghostOffset.X);
                    Canvas.SetTop(_ghostControl, pos.Y - _ghostOffset.Y);
                }

                                if (_activeResizeHandle != null && _selectedControl != null && _designCanvas != null)
                {
                    var currentPos = e.GetPosition(_designCanvas);
                    var deltaX = currentPos.X - _resizeStartPoint.X;
                    var deltaY = currentPos.Y - _resizeStartPoint.Y;
                    
                    var newWidth = _resizeStartSize.Width;
                    var newHeight = _resizeStartSize.Height;
                    var newX = _resizeStartPosition.X;
                    var newY = _resizeStartPosition.Y;
                    
                    // Calculate new dimensions based on direction
                    switch (_resizeDirection)
                    {
                        case "e": // East - width only
                            newWidth = Math.Max(30, _resizeStartSize.Width + deltaX);
                            break;
                        case "w": // West - width and x
                            newWidth = Math.Max(30, _resizeStartSize.Width - deltaX);
                            newX = _resizeStartPosition.X + (_resizeStartSize.Width - newWidth);
                            break;
                        case "s": // South - height only
                            newHeight = Math.Max(20, _resizeStartSize.Height + deltaY);
                            break;
                        case "n": // North - height and y
                            newHeight = Math.Max(20, _resizeStartSize.Height - deltaY);
                            newY = _resizeStartPosition.Y + (_resizeStartSize.Height - newHeight);
                            break;
                        case "se": // Southeast - both
                            newWidth = Math.Max(30, _resizeStartSize.Width + deltaX);
                            newHeight = Math.Max(20, _resizeStartSize.Height + deltaY);
                            break;
                        case "sw": // Southwest
                            newWidth = Math.Max(30, _resizeStartSize.Width - deltaX);
                            newX = _resizeStartPosition.X + (_resizeStartSize.Width - newWidth);
                            newHeight = Math.Max(20, _resizeStartSize.Height + deltaY);
                            break;
                        case "ne": // Northeast
                            newWidth = Math.Max(30, _resizeStartSize.Width + deltaX);
                            newHeight = Math.Max(20, _resizeStartSize.Height - deltaY);
                            newY = _resizeStartPosition.Y + (_resizeStartSize.Height - newHeight);
                            break;
                        case "nw": // Northwest
                            newWidth = Math.Max(30, _resizeStartSize.Width - deltaX);
                            newX = _resizeStartPosition.X + (_resizeStartSize.Width - newWidth);
                            newHeight = Math.Max(20, _resizeStartSize.Height - deltaY);
                            newY = _resizeStartPosition.Y + (_resizeStartSize.Height - newHeight);
                            break;
                    }
                    
                    // Update the control
                    _selectedControl.Width = newWidth;
                    _selectedControl.Height = newHeight;
                    _selectedControl.X = newX;
                    _selectedControl.Y = newY;
                    
                    // Update the visual
                    var border = _designCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == _selectedControl);
                    if (border != null)
                    {
                        border.Width = newWidth;
                        border.Height = newHeight;
                        Canvas.SetLeft(border, newX);
                        Canvas.SetTop(border, newY);
                    }
                    
                    // Update properties panel
                    _updatingProperties = true;
                    if (_propertyEditors.ContainsKey("width")) _propertyEditors["width"]!.Text = newWidth.ToString("F0");
                    if (_propertyEditors.ContainsKey("height")) _propertyEditors["height"]!.Text = newHeight.ToString("F0");
                    if (_propertyEditors.ContainsKey("x")) _propertyEditors["x"]!.Text = newX.ToString("F0");
                    if (_propertyEditors.ContainsKey("y")) _propertyEditors["y"]!.Text = newY.ToString("F0");
                    _updatingProperties = false;
                    
                    UpdateResizeHandles(_selectedControl);
                    return;
                }
                
                if (_draggedControl != null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && _designCanvas != null)
                {
                    var currentPos = e.GetPosition(_designCanvas);
                    var deltaX = currentPos.X - _dragStartPoint.X;
                    var deltaY = currentPos.Y - _dragStartPoint.Y;
                    _draggedControl.X = _dragStartControlPosition.X + deltaX;
                    _draggedControl.Y = _dragStartControlPosition.Y + deltaY;
                    
                    var border = _designCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == _draggedControl);
                    if (border != null)
                    {
                        Canvas.SetLeft(border, _draggedControl.X);
                        Canvas.SetTop(border, _draggedControl.Y);
                    }
                    
                    _updatingProperties = true;
                    if (_propertyEditors.ContainsKey("x") && _propertyEditors["x"] != null)
                        _propertyEditors["x"]!.Text = _draggedControl.X.ToString("F0");
                    if (_propertyEditors.ContainsKey("y") && _propertyEditors["y"] != null)
                        _propertyEditors["y"]!.Text = _draggedControl.Y.ToString("F0");
                    _updatingProperties = false;
                }
            }), RoutingStrategies.Tunnel);
            
            this.AddHandler(PointerReleasedEvent, (EventHandler<PointerReleasedEventArgs>)((s, e) =>
            {
                if (_draggingControlType != null && _ghostControl != null)
                {
                    Console.WriteLine($"🔽 Releasing {_draggingControlType}");
                    
                    if (_designCanvas != null)
                    {
                        var releasePos = e.GetPosition(_designCanvas);
                        
                        if (releasePos.X >= 0 && releasePos.Y >= 0 && 
                            releasePos.X <= _designCanvas.Width && releasePos.Y <= _designCanvas.Height)
                        {
                            var finalX = releasePos.X - _ghostOffset.X;
                            var finalY = releasePos.Y - _ghostOffset.Y;
                            
                            Console.WriteLine($"🎯 Creating at ({finalX}, {finalY})");
                            RemoveGhostControl();
                            AddControlAtPosition(_draggingControlType, finalX, finalY);
                        }
                        else
                        {
                            Console.WriteLine($"❌ Outside canvas");
                            RemoveGhostControl();
                        }
                    }
                    
                    _draggingControlType = null;
                    Console.WriteLine($"✅ Drag complete");
                    e.Handled = true;
                }
                                if (_activeResizeHandle != null && _selectedControl != null)
                {
                    if (_designerDatabase != null)
                    {
                        _designerDatabase.SaveControl(_selectedControl);
                    }
                    UpdateYamlEditor();
                    _activeResizeHandle = null;
                    _resizeDirection = "";
                    Console.WriteLine("✅ Resize complete");
                    e.Handled = true;
                }


                if (_draggedControl != null)
                {
                    if (_designerDatabase != null)
                    {
                        _designerDatabase.SaveControl(_draggedControl);
                    }
                    UpdateYamlEditor();
                    _draggedControl = null;
                }
            }), RoutingStrategies.Tunnel);
            
            canvas.PointerPressed += (s, e) =>
            {
                if (_draggingControlType != null) return;
                
                var clickedElement = e.Source as Control;
                var clickedBorder = clickedElement as Border ?? (clickedElement?.Parent as Border);
                
                if (clickedBorder != null && clickedBorder.Tag is DesignerControl control)
                {
                    SelectControl(control);
                    
                    if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
                    {
                        _draggedControl = control;
                        _dragStartPoint = e.GetPosition(canvas);
                        _dragStartControlPosition = new Point(control.X, control.Y);
                    }
                }
                                else
                {
                    // Clicked canvas background - deselect
                    _selectedControl = null;
                    RemoveResizeHandles();
                    if (_selectedControlLabel != null)
                        _selectedControlLabel.Text = "No control selected";
                    
                    // Remove selection highlighting
                    foreach (var child in canvas.Children.OfType<Border>())
                    {
                        child.BorderBrush = new SolidColorBrush(Color.Parse("#BDBDBD"));
                        child.BorderThickness = new Thickness(1);
                        var controlTag = child.Tag as DesignerControl;
                        child.BoxShadow = controlTag?.Type == "button" ? 
                            new BoxShadows(new BoxShadow { Blur = 2, OffsetY = 1, Color = Color.FromArgb(40, 0, 0, 0) }) : 
                            default;
                    }
                }
            };
        }

        private void CreateGhostControl(string controlType)
        {
            if (_designCanvas == null) return;
            
            RemoveGhostControl();
            
            var width = controlType switch
            {
                "textbox" or "combobox" or "listbox" => 200.0,
                "panel" => 300.0,
                "progressbar" => 250.0,
                _ => 150.0
            };
            
            var height = controlType switch
            {
                "listbox" => 100.0,
                "panel" => 200.0,
                "progressbar" => 25.0,
                _ => 30.0
            };
            
            _ghostControl = new Border
            {
                Width = width,
                Height = height,
                Background = new SolidColorBrush(Color.Parse("#80E3F2FD")),
                BorderBrush = new SolidColorBrush(Color.Parse("#4a90e2")),
                BorderThickness = new Thickness(2),
                Child = new TextBlock
                {
                    Text = controlType.ToUpper(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.Parse("#1976D2")),
                    FontSize = 11,
                    FontWeight = FontWeight.Bold,
                    IsHitTestVisible = false
                },
                IsHitTestVisible = false,
                Opacity = 0.8
            };
            
            _designCanvas.Children.Add(_ghostControl);
            Console.WriteLine($"👻 Ghost created for {controlType}");
        }

        private void RemoveGhostControl()
        {
            if (_ghostControl != null && _designCanvas != null)
            {
                _designCanvas.Children.Remove(_ghostControl);
                _ghostControl = null;
                Console.WriteLine($"🧹 Ghost removed");
            }
        }

        private void AddControlAtPosition(string controlType, double x, double y)
        {
            if (_designCanvas == null) return;
            
            var controlWidth = controlType switch
            {
                "textbox" or "combobox" or "listbox" => 200.0,
                "panel" => 300.0,
                "progressbar" => 250.0,
                _ => 150.0
            };
            
            var controlHeight = controlType switch
            {
                "listbox" => 100.0,
                "panel" => 200.0,
                "progressbar" => 25.0,
                _ => 30.0
            };
            
            if (!_controlCounters.ContainsKey(controlType))
                _controlCounters[controlType] = 1;
            var controlName = $"{controlType}_{_controlCounters[controlType]++}";
            
            var designerControl = new DesignerControl
            {
                Type = controlType,
                Name = controlName,
                X = x,
                Y = y,
                Width = controlWidth,
                Height = controlHeight,
                FontSize = 12,
                Enabled = true,
                Visible = true
            };
            
            switch (controlType)
            {
                case "label": designerControl.Caption = "Label"; break;
                case "button": designerControl.Caption = "Button"; break;
                case "textbox": designerControl.Text = ""; break;
                case "checkbox": designerControl.Caption = "CheckBox"; break;
                case "radiobutton": designerControl.Caption = "RadioButton"; break;
                case "combobox": designerControl.Caption = "ComboBox"; break;
                case "listbox": designerControl.Caption = "ListBox"; break;
                case "panel": designerControl.BackgroundColor = "#f0f0f0"; break;
                case "progressbar": designerControl.Caption = "ProgressBar"; break;
                case "menubar": designerControl.Caption = "MenuBar"; break;
                case "toolbar": designerControl.Caption = "ToolBar"; break;
                case "tabcontrol": designerControl.Caption = "TabControl"; break;
                case "timer": designerControl.Interval = 1000; break;
            }
            
            if (_designerDatabase != null)
            {
                _designerDatabase.SaveControl(designerControl);
            }
            
            var visual = CreateVisualControl(designerControl);
            _designCanvas.Children.Add(visual);
            _designerControls.Add(designerControl);
            UpdateYamlEditor();
            SelectControl(designerControl);
            
            Console.WriteLine($"✅ Created {controlName} at ({x}, {y})");
        }

        private Border CreateVisualControl(DesignerControl control)
        {
            var bgColor = !string.IsNullOrEmpty(control.BackgroundColor) 
                ? Color.Parse(control.BackgroundColor) 
                : control.Type switch
                {
                    "button" => Color.Parse("#e3f2fd"),
                    "textbox" => Colors.White,
                    "panel" => Color.Parse("#fafafa"),
                    _ => Colors.White
                };
            
            var mainBorder = new Border
            {
                Width = control.Width, 
                Height = control.Height,
                Background = new SolidColorBrush(bgColor),
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(1),
                CornerRadius = control.Type == "button" ? new CornerRadius(4) : new CornerRadius(0),
                Tag = control,
                BoxShadow = control.Type == "button" ? new BoxShadows(new BoxShadow { Blur = 2, OffsetY = 1, Color = Color.FromArgb(40, 0, 0, 0) }) : default
            };
            Canvas.SetLeft(mainBorder, control.X);
            Canvas.SetTop(mainBorder, control.Y);
            
            var fgColor = !string.IsNullOrEmpty(control.ForegroundColor)
                ? Color.Parse(control.ForegroundColor)
                : control.Type == "button" ? Color.Parse("#1976D2") : Colors.Black;
            
            var fontSize = control.FontSize ?? 12;
            var fontWeight = control.FontBold ? FontWeight.Bold : (control.Type == "button" ? FontWeight.SemiBold : FontWeight.Normal);
            
            Control innerControl = control.Type switch
            {
                "label" => new TextBlock { 
                    Text = control.Caption ?? control.Name,
                    VerticalAlignment = VerticalAlignment.Center, 
                    Margin = new Thickness(5), 
                    IsHitTestVisible = false,
                    Foreground = new SolidColorBrush(fgColor),
                    FontSize = fontSize,
                    FontWeight = fontWeight
                },
                "button" => new TextBlock { 
                    Text = control.Caption ?? control.Name,
                    VerticalAlignment = VerticalAlignment.Center, 
                    HorizontalAlignment = HorizontalAlignment.Center, 
                    IsHitTestVisible = false,
                    Foreground = new SolidColorBrush(fgColor),
                    FontSize = fontSize,
                    FontWeight = fontWeight
                },
                "textbox" => new TextBlock { 
                    Text = string.IsNullOrEmpty(control.Text) ? control.Name : control.Text,
                    VerticalAlignment = VerticalAlignment.Center, 
                    Margin = new Thickness(5), 
                    Foreground = string.IsNullOrEmpty(control.Text) ? new SolidColorBrush(Color.Parse("#999999")) : new SolidColorBrush(fgColor), 
                    IsHitTestVisible = false,
                    FontSize = fontSize,
                    FontStyle = string.IsNullOrEmpty(control.Text) ? FontStyle.Italic : FontStyle.Normal
                },
                "checkbox" => new TextBlock { 
                    Text = "☐ " + (control.Caption ?? control.Name),
                    VerticalAlignment = VerticalAlignment.Center, 
                    Margin = new Thickness(5), 
                    IsHitTestVisible = false,
                    FontSize = fontSize,
                    Foreground = new SolidColorBrush(fgColor)
                },
                "panel" => new TextBlock {
                    Text = "📦 " + control.Name,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(5),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#999999")),
                    IsHitTestVisible = false
                },
                _ => new TextBlock { 
                    Text = control.Caption ?? control.Name,
                    VerticalAlignment = VerticalAlignment.Center, 
                    Margin = new Thickness(5), 
                    IsHitTestVisible = false,
                    FontSize = fontSize,
                    Foreground = new SolidColorBrush(fgColor)
                }
            };
            
            mainBorder.Child = innerControl;
            AttachContextMenu(control, mainBorder);
            
            return mainBorder;
        }

        private void AttachContextMenu(DesignerControl control, Border border)
        {
            var contextMenu = new ContextMenu();
            
            var scriptsItem = new MenuItem { Header = "📝 Edit Scripts..." };
            scriptsItem.Click += (s, e) => OpenScriptEditor(control);
            contextMenu.Items.Add(scriptsItem);
            
            contextMenu.Items.Add(new Separator());
            
            var deleteItem = new MenuItem { Header = "🗑️ Delete" };
            deleteItem.Click += (s, e) => DeleteControl(control);
            contextMenu.Items.Add(deleteItem);
            
            var duplicateItem = new MenuItem { Header = "📋 Duplicate" };
            duplicateItem.Click += (s, e) => DuplicateControl(control);
            contextMenu.Items.Add(duplicateItem);
            
            contextMenu.Items.Add(new Separator());
            
            var frontItem = new MenuItem { Header = "⬆️ Bring to Front" };
            frontItem.Click += (s, e) => BringToFront(border);
            contextMenu.Items.Add(frontItem);
            
            var backItem = new MenuItem { Header = "⬇️ Send to Back" };
            backItem.Click += (s, e) => SendToBack(border);
            contextMenu.Items.Add(backItem);
            
            border.ContextMenu = contextMenu;
        }

        private void OpenScriptEditor(DesignerControl control)
        {
            if (_scriptDatabase == null)
            {
                Console.WriteLine("❌ Database not initialized!");
                return;
            }
            
            if (!control.DatabaseId.HasValue && _designerDatabase != null)
            {
                _designerDatabase.SaveControl(control);
            }
            
            if (control.DatabaseId.HasValue)
            {
                var scriptEditor = new ScriptEditorWindow(
                    _scriptDatabase,
                    control.DatabaseId.Value,
                    control.Name,
                    control.Type
                );
                
                scriptEditor.Show();
            }
        }

        private void DeleteControl(DesignerControl control)
        {
            if (_designCanvas == null) return;
            
            var border = _designCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == control);
            if (border != null)
            {
                _designCanvas.Children.Remove(border);
                _designerControls.Remove(control);
                
                if (_designerDatabase != null)
                {
                    _designerDatabase.DeleteControl(control);
                }
                
                if (_selectedControl == control)
                {
                    _selectedControl = null;
                    if (_selectedControlLabel != null)
                        _selectedControlLabel.Text = "No control selected";
                }
                
                UpdateYamlEditor();
                Console.WriteLine($"✅ Deleted control: {control.Name}");
            }
        }

        private void DuplicateControl(DesignerControl control)
        {
            AddControlAtPosition(control.Type, control.X + 20, control.Y + 20);
        }

        private void BringToFront(Border border)
        {
            if (_designCanvas == null) return;
            
            _designCanvas.Children.Remove(border);
            _designCanvas.Children.Add(border);
            Console.WriteLine("✅ Brought to front");
        }

        private void SendToBack(Border border)
        {
            if (_designCanvas == null) return;
            
            _designCanvas.Children.Remove(border);
            _designCanvas.Children.Insert(0, border);
            Console.WriteLine("✅ Sent to back");
        }

private void SelectControl(DesignerControl control)
    {
        _selectedControl = control;
        if (_designCanvas != null)
        {
            var borders = _designCanvas.Children.OfType<Border>().ToList();
            
            Border? selectedBorder = null;
            
            foreach (var child in borders)
            {
                if (child.Tag == control)
                {
                    child.BorderBrush = new SolidColorBrush(Color.Parse("#2196F3"));
                    child.BorderThickness = new Thickness(2);
                    child.BoxShadow = new BoxShadows(new BoxShadow 
                    { 
                        Blur = 8, 
                        Color = Color.FromArgb(80, 33, 150, 243),
                        Spread = 1
                    });
                    
                    selectedBorder = child;
                }
                else
                {
                    child.BorderBrush = new SolidColorBrush(Color.Parse("#BDBDBD"));
                    child.BorderThickness = new Thickness(1);
                    var controlTag = child.Tag as DesignerControl;
                    child.BoxShadow = controlTag?.Type == "button" ? 
                        new BoxShadows(new BoxShadow { Blur = 2, OffsetY = 1, Color = Color.FromArgb(40, 0, 0, 0) }) : 
                        default;
                }
            }
            
            if (selectedBorder != null)
            {
                CreateResizeHandles(selectedBorder, control);
            }
        }
        
        if (_selectedControlLabel != null)
        {
            _selectedControlLabel.Text = $"🎯 {control.Name} ({control.Type})";
        }
        
        _updatingProperties = true;
        
        if (_propType != null) _propType.Text = control.Type;
        
        // Load all properties from database
        Dictionary<string, string> controlProps = new Dictionary<string, string>();
        if (control.DatabaseId.HasValue)
        {
            controlProps = LoadControlProperties(control.DatabaseId.Value);
        }
        
        // Get property definitions to know defaults
        var propDefs = GetPropertyDefinitions();
        
        // Populate all property editors from database or defaults
        foreach (var propDef in propDefs)
        {
            var value = GetPropertyValue(controlProps, propDef.PropertyName, propDef.DefaultValue);
            
            if (propDef.EditorType == "textbox" && _propertyEditors.ContainsKey(propDef.PropertyName))
            {
                // Special handling for core properties that come from DesignerControl object
                switch (propDef.PropertyName)
                {
                    case "name":
                        _propertyEditors[propDef.PropertyName]!.Text = control.Name ?? "";
                        break;
                    case "x":
                        _propertyEditors[propDef.PropertyName]!.Text = control.X.ToString("F0");
                        break;
                    case "y":
                        _propertyEditors[propDef.PropertyName]!.Text = control.Y.ToString("F0");
                        break;
                    case "width":
                        _propertyEditors[propDef.PropertyName]!.Text = control.Width.ToString("F0");
                        break;
                    case "height":
                        _propertyEditors[propDef.PropertyName]!.Text = control.Height.ToString("F0");
                        break;
                    case "caption":
                        _propertyEditors[propDef.PropertyName]!.Text = control.Caption ?? "";
                        break;
                    case "text":
                        _propertyEditors[propDef.PropertyName]!.Text = control.Text ?? "";
                        break;
                    default:
                        _propertyEditors[propDef.PropertyName]!.Text = value;
                        break;
                }
            }
            else if (propDef.EditorType == "checkbox" && _propertyCheckboxes.ContainsKey(propDef.PropertyName))
            {
                // Special handling for core boolean properties
                switch (propDef.PropertyName)
                {
                    case "visible":
                        _propertyCheckboxes[propDef.PropertyName]!.IsChecked = control.Visible;
                        break;
                    case "enabled":
                        _propertyCheckboxes[propDef.PropertyName]!.IsChecked = control.Enabled;
                        break;
                    case "font_bold":
                        _propertyCheckboxes[propDef.PropertyName]!.IsChecked = control.FontBold;
                        break;
                    case "font_italic":
                        _propertyCheckboxes[propDef.PropertyName]!.IsChecked = control.FontItalic;
                        break;
                    case "font_underline":
                        _propertyCheckboxes[propDef.PropertyName]!.IsChecked = control.FontUnderline;
                        break;
                    default:
                        _propertyCheckboxes[propDef.PropertyName]!.IsChecked = bool.Parse(value);
                        break;
                }
            }
        }
        
        _updatingProperties = false;
    }


        private void UpdateYamlEditor()
        {
            if (_yamlEditor == null || _importExport == null) return;
            _yamlEditor.Text = _importExport.ExportToYAML();
        }

        private void LaunchPreview()
        {
            if (_designerControls.Count == 0) return;
            if (_importExport == null) return;
            
            var yaml = _importExport.ExportToYAML();
            var tempPath = Path.Combine(Path.GetTempPath(), "preview-form.yaml");
            File.WriteAllText(tempPath, yaml);
            
            var previewWindow = new MainWindow(tempPath);
            previewWindow.Show();
        }

        private void LoadAllFromDatabase()
        {
            if (_designerDatabase == null || _designCanvas == null) return;
            
            var controls = _designerDatabase.LoadAllFromDatabase();
            
            _designerControls.Clear();
            _designCanvas.Children.Clear();
            
            foreach (var control in controls)
            {
                var visual = CreateVisualControl(control);
                _designCanvas.Children.Add(visual);
                _designerControls.Add(control);
            }
            
            UpdateYamlEditor();
        }

        private void ResetDatabase()
        {
            if (_designerDatabase == null || _designCanvas == null) return;
            
            _designerDatabase.ResetDatabase();
            _designerControls.Clear();
            _designCanvas.Children.Clear();
            UpdateYamlEditor();
        }

        private Expander CreateToolboxGroup(string header, Color color)
        {
            var expander = new Expander
            {
                IsExpanded = true,
                Margin = new Thickness(0, 0, 0, 8)
            };
            
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B)),
                Padding = new Thickness(8, 6, 8, 6),
                Child = new TextBlock
                {
                    Text = header,
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(color)
                }
            };
            
            expander.Header = headerBorder;
            return expander;
        }

        private Expander CreatePropertyGroup(string header, Color color)
        {
            var expander = new Expander
            {
                IsExpanded = true,
                Margin = new Thickness(0, 0, 0, 5)
            };
            
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B)),
                Padding = new Thickness(8, 4, 8, 4),
                Child = new TextBlock
                {
                    Text = header,
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(color)
                }
            };
            
            expander.Header = headerBorder;
            return expander;
        }

        private void SetupCanvasSizeMenu(Menu menu)
        {
            foreach (var topItem in menu.Items.Cast<MenuItem>())
            {
                if (topItem.Header?.ToString() == "View")
                {
                    var canvasSizeMenu = new MenuItem { Header = "Canvas Size" };
                    
                    AddCanvasSizeMenuItem(canvasSizeMenu, "800x600 (Default)", 800, 600);
                    AddCanvasSizeMenuItem(canvasSizeMenu, "1024x768 (XGA)", 1024, 768);
                    AddCanvasSizeMenuItem(canvasSizeMenu, "1280x720 (HD)", 1280, 720);
                    AddCanvasSizeMenuItem(canvasSizeMenu, "1920x1080 (Full HD)", 1920, 1080);
                    
                    canvasSizeMenu.Items.Add(new Separator());
                    
                    AddCanvasSizeMenuItem(canvasSizeMenu, "375x667 (iPhone SE)", 375, 667);
                    AddCanvasSizeMenuItem(canvasSizeMenu, "390x844 (iPhone 13)", 390, 844);
                    AddCanvasSizeMenuItem(canvasSizeMenu, "412x915 (Android)", 412, 915);
                    AddCanvasSizeMenuItem(canvasSizeMenu, "768x1024 (iPad)", 768, 1024);
                    
                    canvasSizeMenu.Items.Add(new Separator());
                    
                    var customItem = new MenuItem { Header = "Custom Size..." };
                    customItem.Click += async (s, e) => await ShowCustomCanvasSizeDialog();
                    canvasSizeMenu.Items.Add(customItem);
                    
                    topItem.Items.Add(canvasSizeMenu);
                    break;
                }
            }
        }
        
        private void AddCanvasSizeMenuItem(MenuItem parent, string label, double width, double height)
        {
            var item = new MenuItem { Header = label };
            item.Click += (s, e) => SetCanvasSize(width, height);
            parent.Items.Add(item);
        }
        
        private void SetCanvasSize(double width, double height)
        {
            if (_designCanvas == null) return;
            
            _designCanvas.Width = width;
            _designCanvas.Height = height;
            
            Console.WriteLine($"✅ Canvas resized to {width}x{height}");
        }
        
        private async System.Threading.Tasks.Task ShowCustomCanvasSizeDialog()
        {
            var dialog = new Window
            {
                Title = "Custom Canvas Size",
                Width = 350,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };
            
            var stack = new StackPanel
            {
                Margin = new Thickness(20)
            };
            
            var title = new TextBlock
            {
                Text = "Enter custom canvas dimensions:",
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stack.Children.Add(title);
            
            var widthPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };
            widthPanel.Children.Add(new TextBlock { Text = "Width:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            var widthBox = new TextBox { Width = 100, Text = _designCanvas?.Width.ToString() ?? "800" };
            widthPanel.Children.Add(widthBox);
            widthPanel.Children.Add(new TextBlock { Text = " px", Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            stack.Children.Add(widthPanel);
            
            var heightPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
            };
            heightPanel.Children.Add(new TextBlock { Text = "Height:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            var heightBox = new TextBox { Width = 100, Text = _designCanvas?.Height.ToString() ?? "600" };
            heightPanel.Children.Add(heightBox);
            heightPanel.Children.Add(new TextBlock { Text = " px", Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            stack.Children.Add(heightPanel);
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0)
            };
            okButton.Click += (s, e) =>
            {
                if (double.TryParse(widthBox.Text, out double w) && double.TryParse(heightBox.Text, out double h))
                {
                    SetCanvasSize(w, h);
                    dialog.Close();
                }
            };
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80
            };
            cancelButton.Click += (s, e) => dialog.Close();
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(buttonPanel);
            
            dialog.Content = stack;
            await dialog.ShowDialog(this);
        }
    }
}	
