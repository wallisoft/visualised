using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Microsoft.Data.Sqlite;

namespace VB;

public class DesignerWindow
{
    // ========================================
    // CORE FIELDS
    // ========================================
    private static Window? mainWindow;
    private static Canvas? designCanvas;
    private static Control? selectedControl;
    private static Rectangle? selectionBorder;
    private static Rectangle? designOverlay;
    private static PropertiesPanel? propertiesPanel;
    private static TextBlock? statusText;
    private static Border? formBuilderBorder;
    private static Dictionary<string, int> controlCounters = new();
    
    // Drag state
    private static bool isDragging = false;
    private static Point dragStart;
    private static double dragStartX, dragStartY;
    private static string? resizeEdge = null;

    // ========================================
    // ENTRY POINT
    // ========================================
    public static void LoadAndApply(MainWindow window, string vmlPath)
    {
        PropertyStore.Initialize();
        
        // Parse VML into database
        var parser = new VmlDatabaseParser(PropertyStore.GetDbPath());
        parser.ImportVml(vmlPath);
        
        // Build designer UI from database
        BuildUI(window, vmlPath);
    }

    // ========================================
    // BUILD MAIN UI FROM VML
    // ========================================
    private static void BuildUI(MainWindow window, string vmlPath)
    {
        mainWindow = window;  

        // Load designer UI from VML
        var dbPath = PropertyStore.GetDbPath();
        var root = LoadControlTreeFromDatabase(dbPath);
        var overlayWidth = Settings.GetDouble("overlay_width", 800);
        var overlayHeight = Settings.GetDouble("overlay_height", 600);
        var formBuilderWidth = Settings.GetDouble("formbuilder_width", 300);

        // If root is a Window, apply its properties to our MainWindow
        if (root is Window vmlWindow)
        {
            window.Width = vmlWindow.Width;
            window.Height = vmlWindow.Height;
            if (vmlWindow.Title != null) window.Title = vmlWindow.Title;

            // Use the Window's content (should be MainGrid)
            root = (vmlWindow.Content as Control) ?? root;
        }
        
        // Find key controls by name in tree
        var mainGrid = root as Grid; 
        var workspace = FindControlInTree<Grid>(root, "Workspace");
        var formBuilderStack = FindControlInTree<StackPanel>(root, "FormBuilderStack");
        var selectorRow = FindControlInTree<StackPanel>(root, "SelectorRow");
        var propertiesStack = FindControlInTree<StackPanel>(root, "propertiesStack");
        statusText = FindControlInTree<TextBlock>(root, "statusText");
        formBuilderBorder = FindControlInTree<Border>(root, "formBuilderBorder");
        
        // ========================================
        // ADD CONTROL SELECTOR (Code-only)
        // ========================================
        if (selectorRow != null)
        {
            var controlSelector = new ComboBox
            {
                Width = 130,
                Height = 32,
                FontSize = 13,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.Parse("#107C10")),
                BorderThickness = new Thickness(2)
            };

            controlSelector.Items.Add("Button");
            controlSelector.Items.Add("TextBox");
            controlSelector.Items.Add("TextBlock");
            controlSelector.Items.Add("CheckBox");
            controlSelector.Items.Add("ComboBox");
            controlSelector.Items.Add("ListBox");
            controlSelector.Items.Add("RadioButton");
            controlSelector.Items.Add("StackPanel");
            controlSelector.Items.Add("Grid");
            controlSelector.Items.Add("Border");
            controlSelector.Items.Add("─────────");
            controlSelector.Items.Add("MainWindow");
            controlSelector.Text = "Button";

            string selectedControlType = "Button";

            controlSelector.SelectedIndex = 0;

            controlSelector.SelectionChanged += async (s, e) =>
            {
                if (controlSelector.SelectedItem?.ToString() == "MainWindow")
                {
                    await ShowMainWindowWarning(mainWindow!);
                    controlSelector.SelectedIndex = 0;
                    return;
                }
                selectedControlType = controlSelector.SelectedItem?.ToString() ?? "Button";
            };

            var addBtn = new Button
            {
                Content = " Add ",
                Width = 70,
                Height = 32,
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Background = new SolidColorBrush(Color.Parse("#107C10")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.Parse("#0d6b0d")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            addBtn.Click += (s, e) => AddControlToCanvas(selectedControlType);

            controlSelector.SelectionChanged += async (s, selected) =>
            {
                if (selected?.ToString() == "MainWindow")
                {
                    await ShowMainWindowWarning(window);
                    controlSelector.Text = "";
                    return;
                }
                selectedControlType = selected?.ToString() ?? "Button";
            };
            
            selectorRow.Children.Add(controlSelector);
            selectorRow.Children.Add(addBtn);
        }
        
        // ========================================
        // ADD CANVAS WITH OVERLAY (Code-only)
        // ========================================
        if (workspace != null)
        {
            var canvasScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetColumn(canvasScroll, 1);
            
            designCanvas = new Canvas
            {
                Width = 4000,
                Height = 4000,
                Background = new SolidColorBrush(Color.Parse("#e8f5e9")) 
            };

            // Register FIRST, before any controls added
            designCanvas.PointerMoved += (s, e) =>
            {
                var mousePos = e.GetPosition(designCanvas);
                var offsetX = (int)mousePos.X - 150;
                var offsetY = (int)mousePos.Y - 100;

                if (statusText != null && mainWindow != null)
                {
                    var controlName = selectedControl?.Name ?? "None";
                    var winW = (int)mainWindow.ClientSize.Width;
                    var winH = (int)mainWindow.ClientSize.Height;
                    statusText.Text = $"Selected: {controlName} | Window: {winW}x{winH} | Mouse: {offsetX},{offsetY}";
                }
            };
            
            // Calculate viewport (window - formbuilder - bars)
            var menuHeight = 30.0;
            var statusHeight = 25.0;
            var viewportWidth = mainWindow!.Width - formBuilderWidth;
            var viewportHeight = mainWindow.Height - menuHeight - statusHeight;
            
            // Center overlay in viewport
            var overlayX = (viewportWidth - overlayWidth) / 2;
            var overlayY = (viewportHeight - overlayHeight) / 2;
            
            // Create overlay rectangle
            designOverlay = new Rectangle
            {
                Width = overlayWidth,
                Height = overlayHeight,
                Fill = new SolidColorBrush(Color.Parse("#e8f5e9")),  
                Stroke = new SolidColorBrush(Color.Parse("#66bb6a")),
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            
            Canvas.SetLeft(designOverlay, overlayX);
            Canvas.SetTop(designOverlay, overlayY);
            designCanvas.Children.Add(designOverlay);
            
            // Overlay label
            var overlayLabel = new TextBlock
            {
                Text = $"{(int)overlayWidth}x{(int)overlayHeight} Design Area",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#66bb6a")),
                Background = Brushes.Transparent,
                Padding = new Thickness(10, 5),
                IsHitTestVisible = false
            };
            
            Canvas.SetLeft(overlayLabel, overlayX + 10);
            Canvas.SetTop(overlayLabel, overlayY + 10);
            designCanvas.Children.Add(overlayLabel);
            
            // Selection border
            selectionBorder = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                IsHitTestVisible = false,
                IsVisible = false
            };
            designCanvas.Children.Add(selectionBorder);
            
            canvasScroll.Content = designCanvas;
            workspace.Children.Add(canvasScroll);
        }
        
        // ========================================
        // INITIALIZE PROPERTIES PANEL
        // ========================================
        if (propertiesStack != null)
        {
            propertiesPanel = new PropertiesPanel(propertiesStack);
            propertiesPanel.PanelCloseRequested += (s, e) => 
            {
                if (formBuilderBorder != null)
                    formBuilderBorder.IsVisible = false;
            };
        }
        
        // ========================================
        // WIRE CLOSE BUTTON
        // ========================================
        var closeBtn = FindControlInTree<Button>(root, "closePanelBtn");
        if (closeBtn != null)
        {
            closeBtn.Click += (s, e) => 
            {
                if (formBuilderBorder != null)
                    formBuilderBorder.IsVisible = false;
            };
        }
        
        // Set window content
        window.Content = mainGrid;
        
        // Load controls from PropertyStore
        LoadPropertyStoreControls();

        // Wire canvas mouse movement to status bar
        if (designCanvas != null)
        {
            designCanvas.PointerMoved += (s, e) =>
            {
                var mousePos = e.GetPosition(designCanvas);
                var offsetX = (int)mousePos.X - 150;
                var offsetY = (int)mousePos.Y - 100;

                if (statusText != null && mainWindow != null)
                {
                    var controlName = selectedControl?.Name ?? "None";
                    var winW = (int)mainWindow.ClientSize.Width;
                    var winH = (int)mainWindow.ClientSize.Height;
                    statusText.Text = $"Selected: {controlName} | Window: {winW}x{winH} | Mouse: {offsetX},{offsetY}";
                }
            };
        }

        window.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "ClientSize" || e.Property.Name == "Bounds")
                UpdateStatusBar();  // No parameter
        };

        UpdateStatusBar();  // No parameter

        Console.WriteLine("[UI] Designer ready!");
    }

    // ========================================
    // HELPER: FIND CONTROL IN VISUAL TREE
    // ========================================
    private static T? FindControlInTree<T>(Control? root, string name) where T : Control
    {
        if (root == null) return null;
        if (root is T match && root.Name == name) return match;

        // Search children recursively
        if (root is Panel panel)
        {
            foreach (var child in panel.Children.OfType<Control>())
            {
                var found = FindControlInTree<T>(child, name);
                if (found != null) return found;
            }
        }
        else if (root is ContentControl contentControl && contentControl.Content is Control childControl)
        {
            return FindControlInTree<T>(childControl, name);
        }
        else if (root is Decorator decorator && decorator.Child is Control decoratorChild)
        {
            return FindControlInTree<T>(decoratorChild, name);
        }
        else if (root is ScrollViewer scrollViewer && scrollViewer.Content is Control scrollChild)
        {
            return FindControlInTree<T>(scrollChild, name);
        }

        return null;
    }

    // ========================================
    // LOAD CONTROL TREE FROM DATABASE
    // ========================================
    private static Control LoadControlTreeFromDatabase(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        // Get root control
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, control_type, name FROM ui_tree WHERE is_root = 1 OR parent_id IS NULL LIMIT 1";
        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            throw new Exception("No root control found in ui_tree");

        var rootId = reader.GetInt32(0);
        var rootType = reader.GetString(1);
        var rootName = reader.IsDBNull(2) ? null : reader.GetString(2);
        reader.Close();

        Console.WriteLine($"[DB] Loading root: {rootType} ({rootName})");

        // Build tree recursively
        return BuildControlFromDatabase(conn, rootId, rootType, rootName);
    }

    // ========================================
    // BUILD CONTROL RECURSIVELY FROM DATABASE
    // ========================================
    private static Control BuildControlFromDatabase(SqliteConnection conn, int id, string controlType, string? name)
    {
        // Create control
        Control control = controlType switch
        {
            "Window" => new Window(),
            "Grid" => new Grid(),
            "Border" => new Border(),
            "StackPanel" => new StackPanel(),
            "DockPanel" => new DockPanel(),
            "TextBlock" => new TextBlock(),
            "Button" => new Button(),
            "TextBox" => new TextBox(),
            "ScrollViewer" => new ScrollViewer(),
            "Panel" => new Panel(),
            _ => throw new Exception($"Unknown control type: {controlType}")
        };

        if (name != null) control.Name = name;

        Console.WriteLine($"[DB] Building {controlType} '{name ?? "unnamed"}'");

        // Load properties
        using var propCmd = conn.CreateCommand();
        propCmd.CommandText = "SELECT property_name, property_value FROM ui_properties WHERE ui_tree_id = @id";
        propCmd.Parameters.AddWithValue("@id", id);

        using var propReader = propCmd.ExecuteReader();
        while (propReader.Read())
        {
            var propName = propReader.GetString(0);
            var propValue = propReader.GetString(1);
            ApplyPropertyFromDatabase(control, propName, propValue);
        }
        propReader.Close();

        // Load children
        using var childCmd = conn.CreateCommand();
        childCmd.CommandText = "SELECT id, control_type, name FROM ui_tree WHERE parent_id = @id ORDER BY display_order";
        childCmd.Parameters.AddWithValue("@id", id);

        using var childReader = childCmd.ExecuteReader();
        var children = new List<(int id, string type, string? name)>();

        while (childReader.Read())
        {
            children.Add((
                childReader.GetInt32(0),
                childReader.GetString(1),
                childReader.IsDBNull(2) ? null : childReader.GetString(2)
            ));
        }
        childReader.Close();

        // Recursively build children
        foreach (var (childId, childType, childName) in children)
        {
            var child = BuildControlFromDatabase(conn, childId, childType, childName);

            if (control is Panel panel)
                panel.Children.Add(child);
            else if (control is ContentControl content)
                content.Content = child;
            else if (control is Decorator decorator)
                decorator.Child = child;
            else if (control is ScrollViewer scroll)
                scroll.Content = child;
        }

        return control;
    }

    // ========================================
    // APPLY PROPERTY FROM DATABASE
    // ========================================
    private static void ApplyPropertyFromDatabase(Control control, string propertyName, string propertyValue)
    {
        try
        {
            if (propertyName.Contains("."))
            {
                var parts = propertyName.Split('.');
                var ownerType = parts[0];
                var propName = parts[1];

                if (ownerType == "Grid")
                {
                    if (propName == "Row" && int.TryParse(propertyValue, out var row))
                    {
                        Grid.SetRow(control, row);
                        Console.WriteLine($"[DB]   Set Grid.Row = {row}");
                        return;
                    }
                    if (propName == "Column" && int.TryParse(propertyValue, out var col))
                    {
                        Grid.SetColumn(control, col);
                        Console.WriteLine($"[DB]   Set Grid.Column = {col}");
                        return;
                    }
                }
                else if (ownerType == "DockPanel" && propName == "Dock")
                {
                    var dock = Enum.Parse(typeof(Dock), propertyValue);
                    DockPanel.SetDock(control, (Dock)dock);
                    Console.WriteLine($"[DB]   Set DockPanel.Dock = {dock}");
                    return;
                }
            }
            var prop = control.GetType().GetProperty(propertyName);
            if (prop == null || !prop.CanWrite) return;
            
            // Type conversion
            object? value = prop.PropertyType.Name switch
            {
                "Double" => double.Parse(propertyValue),
                "Int32" => int.Parse(propertyValue),
                "Boolean" => bool.Parse(propertyValue),
                "String" => propertyValue,
                "IBrush" => Brush.Parse(propertyValue),
                "Thickness" => Thickness.Parse(propertyValue),
                "CornerRadius" => CornerRadius.Parse(propertyValue),
                "RowDefinitions" => RowDefinitions.Parse(propertyValue),
                "ColumnDefinitions" => ColumnDefinitions.Parse(propertyValue),
                "HorizontalAlignment" => Enum.Parse(typeof(HorizontalAlignment), propertyValue),
                "VerticalAlignment" => Enum.Parse(typeof(VerticalAlignment), propertyValue),
                "Orientation" => Enum.Parse(typeof(Orientation), propertyValue),
                "ScrollBarVisibility" => Enum.Parse(typeof(ScrollBarVisibility), propertyValue),
                "FontWeight" => propertyValue.ToLower() switch
                {
                    "bold" => FontWeight.Bold,
                    "normal" => FontWeight.Normal,
                    "light" => FontWeight.Light,
                    _ => FontWeight.Normal
                },
                _ => Convert.ChangeType(propertyValue, prop.PropertyType)
            };
            
            prop.SetValue(control, value);
            Console.WriteLine($"[DB]   Set {propertyName} = {propertyValue}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB]   Failed to apply {propertyName}={propertyValue}: {ex.Message}");
        }
    }

    // ========================================
    // SHOW MAINWINDOW WARNING
    // ========================================
    private static async System.Threading.Tasks.Task ShowMainWindowWarning(Window owner)
    {
        var warningStack = new StackPanel
        {
            Spacing = 15,
            Margin = new Thickness(20),
            Children =
            {
                new TextBlock 
                { 
                    Text = "MainWindow can only be created once per application.",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13
                },
                new Button 
                { 
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Width = 80,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
                    BorderThickness = new Thickness(2)
                }
            }
        };
        
        var warning = new Window
        {
            Title = "Cannot Add MainWindow",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = warningStack
        };
        
        ((Button)warningStack.Children[1]).Click += (s2, e2) => warning.Close();
        await warning.ShowDialog(owner);
    }

    // ========================================
    // LOAD PROPERTYSTORE CONTROLS
    // ========================================
    private static void LoadPropertyStoreControls()
    {
        if (designCanvas == null) return;
        
        var dbPath = PropertyStore.GetDbPath();
        using (var conn = new SqliteConnection($"Data Source={dbPath}"))
        {
            conn.Open();
            
            // Clear previous session
            var clearCmd = conn.CreateCommand();
            clearCmd.CommandText = "DELETE FROM properties WHERE substr(control_name, 1, 1) != '_'";
            clearCmd.ExecuteNonQuery();
            
            // Query remaining controls
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT control_name FROM properties WHERE substr(control_name, 1, 1) != '_'";
            using var reader = cmd.ExecuteReader();
            
            var savedControls = new List<string>();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                if (designCanvas.Children.OfType<Control>().Any(c => c.Name == name))
                    continue;
                savedControls.Add(name);
            }
            
            reader.Close();
            
            foreach (var controlName in savedControls)
            {
                var controlType = controlName.Contains('_') ? controlName.Split('_')[0] : "Button";
                var (dummy, real) = CreateControlPair(controlType, controlName);
                if (dummy == null) continue;
                
                var savedProps = PropertyStore.GetControlProperties(controlName);
                
                double x = 200, y = 200, w = 0, h = 0;
                
                foreach (var kvp in savedProps)
                {
                    if (kvp.Value == null) continue;
                    
                    if (kvp.Key == "X") { double.TryParse(kvp.Value.ToString(), out x); continue; }
                    if (kvp.Key == "Y") { double.TryParse(kvp.Value.ToString(), out y); continue; }
                    if (kvp.Key == "Width") { double.TryParse(kvp.Value.ToString(), out w); continue; }
                    if (kvp.Key == "Height") { double.TryParse(kvp.Value.ToString(), out h); continue; }
                    
                    try
                    {
                        var propInfo = real.GetType().GetProperty(kvp.Key);
                        if (propInfo != null && propInfo.CanWrite)
                        {
                            var value = Convert.ChangeType(kvp.Value, propInfo.PropertyType);
                            propInfo.SetValue(real, value);
                            
                            if (kvp.Key == "Content" || kvp.Key == "Text")
                                dummy.GetType().GetProperty(kvp.Key)?.SetValue(dummy, kvp.Value);
                        }
                    }
                    catch { }
                }
                
                Canvas.SetLeft(dummy, x);
                Canvas.SetTop(dummy, y);
                Canvas.SetLeft(real, x);
                Canvas.SetTop(real, y);
                
                if (w > 0) { dummy.Width = w; real.Width = w; }
                if (h > 0) { dummy.Height = h; real.Height = h; }
                
                designCanvas.Children.Add(dummy);
                designCanvas.Children.Add(real);
                MakeDraggable(dummy);
            }
        }
    }

    // ========================================
    // CREATE CONTROL PAIR
    // ========================================
    private static (Control? dummy, Control? real) CreateControlPair(string controlType, string name)
    {
        Control? dummy = controlType switch 
        {
            "Button" => new DesignButton(),
            "TextBox" => new DesignTextBox(),
            "TextBlock" => new DesignTextBlock(),
            "CheckBox" => new DesignCheckBox(),
            "ComboBox" => new DesignComboBox(),
            "ListBox" => new DesignListBox(),
            "RadioButton" => new DesignRadioButton(),
            "StackPanel" => new DesignPanel("StackPanel"),
            "Grid" => new DesignPanel("Grid"),
            "Border" => new DesignBorder(),
            _ => null
        };
        
        Control? real = controlType switch 
        {
            "Button" => new Button { Content = "Button" },
            "TextBox" => new TextBox { Text = "TextBox" },
            "TextBlock" => new TextBlock { Text = "Label" },
            "CheckBox" => new CheckBox { Content = "CheckBox" },
            "ComboBox" => new ComboBox(),
            "ListBox" => new ListBox(),
            "RadioButton" => new RadioButton { Content = "RadioButton" },
            "StackPanel" => new StackPanel(),
            "Grid" => new Grid(),
            "Border" => new Border(),
            _ => null
        };
        
        if (dummy != null && real != null)
        {
            dummy.Name = name;
            real.Name = name;
            real.IsVisible = false;
            real.IsEnabled = false;
            real.IsHitTestVisible = false;
            dummy.Tag = real;
        }
        
        return (dummy, real);
    }

    // ========================================
    // ADD CONTROL TO CANVAS
    // ========================================
    private static void AddControlToCanvas(string controlType)
    {
        if (designCanvas == null) return;
        
        // Generate name
        if (!controlCounters.ContainsKey(controlType))
            controlCounters[controlType] = 0;
        
        controlCounters[controlType]++;
        var controlName = $"{controlType}_{controlCounters[controlType]}";
        
        var (dummy, real) = CreateControlPair(controlType, controlName);
        if (dummy == null || real == null) return;
        
        // Stack at 200,200 with offset to avoid overlap
        var baseX = 200.0;
        var baseY = 200.0;
        var offset = 0;
        
        while (true)
        {
            var testX = baseX + (offset * 20);
            var testY = baseY + (offset * 20);
            
            var occupied = designCanvas.Children.OfType<Control>()
                .Any(c => c != designOverlay && c != selectionBorder && 
                     Math.Abs(Canvas.GetLeft(c) - testX) < 5 && 
                     Math.Abs(Canvas.GetTop(c) - testY) < 5);
            
            if (!occupied)
            {
                Canvas.SetLeft(dummy, testX);
                Canvas.SetTop(dummy, testY);
                Canvas.SetLeft(real, testX);
                Canvas.SetTop(real, testY);
                
                designCanvas.Children.Add(dummy);
                designCanvas.Children.Add(real);
                MakeDraggable(dummy);
                SelectControl(dummy);
                PropertyStore.SyncControl(real);
                
                Console.WriteLine($"[DESIGNER] Added {controlType} '{controlName}' at ({testX},{testY})");
                return;
            }
            offset++;
            if (offset > 20) break; // Safety limit
        }
    }

    // ========================================
    // SELECTION
    // ========================================
    private static void SelectControl(Control control)
    {
        selectedControl = control;
        propertiesPanel?.ShowPropertiesFor(control);
        UpdateSelectionBorder();
        UpdateStatusBar();
    }
    
    private static void UpdateSelectionBorder()
    {
        if (selectedControl == null || selectionBorder == null || designCanvas == null)
        {
            if (selectionBorder != null) selectionBorder.IsVisible = false;
            return;
        }
        
        var x = Canvas.GetLeft(selectedControl);
        var y = Canvas.GetTop(selectedControl);
        var w = selectedControl.Bounds.Width;
        var h = selectedControl.Bounds.Height;
        
        Canvas.SetLeft(selectionBorder, x - 2);
        Canvas.SetTop(selectionBorder, y - 2);
        selectionBorder.Width = w + 4;
        selectionBorder.Height = h + 4;
        selectionBorder.IsVisible = true;
        
        // Bring to front
        designCanvas.Children.Remove(selectedControl);
        designCanvas.Children.Add(selectedControl);
        designCanvas.Children.Remove(selectionBorder);
        designCanvas.Children.Add(selectionBorder);
    }
    
    private static void UpdateStatusBar()
    {
        if (statusText == null || mainWindow == null) return;
        
        var controlName = selectedControl?.Name ?? "None";
        var winW = (int)mainWindow.ClientSize.Width;
        var winH = (int)mainWindow.ClientSize.Height;
        
        statusText.Text = $"Selected: {controlName} | Window: {winW}x{winH}";  // Remove mouse - it's updated in PointerMoved
    }

    // ========================================
    // DRAG & RESIZE
    // ========================================
    private static void MakeDraggable(Control control)
    {
        control.PointerPressed += (s, e) =>
        {
            if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed) return;
                
            var pos = e.GetPosition(control);
            var zone = GetResizeZone(control, pos);

                Console.WriteLine($"[MOUSE] Pressed at {pos.X},{pos.Y} - zone: {zone ?? "null"}");
            
            if (zone != null)
            {
                // Start resize
                resizeEdge = zone;
                isDragging = false;  // Clear drag flag

                Console.WriteLine($"[MOUSE] Starting RESIZE mode: {zone}");

                dragStartX = Canvas.GetLeft(control);
                dragStartY = Canvas.GetTop(control);
                dragStart = e.GetPosition(designCanvas);
                control.Cursor = GetCursorForZone(zone);
            }
            else
            {
                // Start move
                isDragging = true;
                resizeEdge = null;  // Clear resize flag
                        Console.WriteLine($"[MOUSE] Starting DRAG mode");
                dragStart = e.GetPosition(designCanvas);
                dragStartX = Canvas.GetLeft(control);
                dragStartY = Canvas.GetTop(control);
                control.Cursor = new Cursor(StandardCursorType.SizeAll);
            }
            
            SelectControl(control);
            e.Pointer.Capture(control);  // CAPTURE POINTER
            e.Handled = true;
        };

        control.PointerMoved += (s, e) =>
        {
                    Console.WriteLine($"[MOUSE] Resizing: {resizeEdge}");
            if (resizeEdge != null)
            {
                // Handle resize
                        Console.WriteLine($"[MOUSE] Resizing: {resizeEdge}");
                var current = e.GetPosition(designCanvas);
                var deltaX = current.X - dragStart.X;
                var deltaY = current.Y - dragStart.Y;
                
                HandleResize(control, resizeEdge, deltaX, deltaY, dragStartX, dragStartY);
                UpdateSelectionBorder();
                e.Handled = true;
            }
            else if (isDragging)
            {
                // Handle move
                        Console.WriteLine($"[MOUSE] Dragging");
                var current = e.GetPosition(designCanvas);
                var deltaX = current.X - dragStart.X;
                var deltaY = current.Y - dragStart.Y;
                
                Canvas.SetLeft(control, dragStartX + deltaX);
                Canvas.SetTop(control, dragStartY + deltaY);
                
                if (control.Tag is Control real)
                {
                    Canvas.SetLeft(real, dragStartX + deltaX);
                    Canvas.SetTop(real, dragStartY + deltaY);
                }
                
                UpdateSelectionBorder();
                e.Handled = true;
            }
            else
            {
                // Just hovering - update cursor
                var pos = e.GetPosition(control);
                var zone = GetResizeZone(control, pos);
                control.Cursor = GetCursorForZone(zone);
            }
        };

        control.PointerReleased += (s, e) =>
        {
            if (isDragging || resizeEdge != null)
            {
                // Save position/size
                if (control.Tag is Control real)
                {
                    PropertyStore.Set(real.Name!, "X", Canvas.GetLeft(control).ToString());
                    PropertyStore.Set(real.Name!, "Y", Canvas.GetTop(control).ToString());
                    PropertyStore.Set(real.Name!, "Width", control.Bounds.Width.ToString());
                    PropertyStore.Set(real.Name!, "Height", control.Bounds.Height.ToString());
                }
                
                isDragging = false;
                resizeEdge = null;
                control.Cursor = new Cursor(StandardCursorType.Arrow);
            }
            
            e.Pointer.Capture(null);  // RELEASE CAPTURE
            e.Handled = true;
        };

        control.PointerCaptureLost += (s, e) =>
        {
            // Safety: reset if capture lost
            isDragging = false;
            resizeEdge = null;
            control.Cursor = new Cursor(StandardCursorType.Arrow);
        };
        }

        private static string? GetResizeZone(Control control, Point pos)
        {
            const double edgeSize = 8;
            var w = control.Bounds.Width;
            var h = control.Bounds.Height;
            
            bool onLeft = pos.X <= edgeSize;
            bool onRight = pos.X >= w - edgeSize;
            bool onTop = pos.Y <= edgeSize;
            bool onBottom = pos.Y >= h - edgeSize;

            if (onTop && onLeft) return "NW";
            if (onTop && onRight) return "NE";
            if (onBottom && onLeft) return "SW";
            if (onBottom && onRight) return "SE";
            if (onTop) return "N";
            if (onBottom) return "S";
            if (onLeft) return "W";
            if (onRight) return "E";
            
        return null;
    }
    
    private static Cursor GetCursorForZone(string? zone)
    {
        return zone switch
        {
            "NW" or "SE" => new Cursor(StandardCursorType.TopLeftCorner),
            "NE" or "SW" => new Cursor(StandardCursorType.TopRightCorner),
            "N" or "S" => new Cursor(StandardCursorType.SizeNorthSouth),
            "W" or "E" => new Cursor(StandardCursorType.SizeWestEast),
            _ => new Cursor(StandardCursorType.Arrow)
        };
    }
    
    private static void HandleResize(Control control, string edge, double deltaX, double deltaY, double startX, double startY)
    {
        var w = control.Width;
        var h = control.Height;
        
        switch (edge)
        {
            case "E":
                control.Width = Math.Max(20, w + deltaX);
                break;
            case "W":
                control.Width = Math.Max(20, w - deltaX);
                Canvas.SetLeft(control, startX + deltaX);
                break;
            case "S":
                control.Height = Math.Max(20, h + deltaY);
                break;
            case "N":
                control.Height = Math.Max(20, h - deltaY);
                Canvas.SetTop(control, startY + deltaY);
                break;
            case "SE":
                control.Width = Math.Max(20, w + deltaX);
                control.Height = Math.Max(20, h + deltaY);
                break;
            case "SW":
                control.Width = Math.Max(20, w - deltaX);
                control.Height = Math.Max(20, h + deltaY);
                Canvas.SetLeft(control, startX + deltaX);
                break;
            case "NE":
                control.Width = Math.Max(20, w + deltaX);
                control.Height = Math.Max(20, h - deltaY);
                Canvas.SetTop(control, startY + deltaY);
                break;
            case "NW":
                control.Width = Math.Max(20, w - deltaX);
                control.Height = Math.Max(20, h - deltaY);
                Canvas.SetLeft(control, startX + deltaX);
                Canvas.SetTop(control, startY + deltaY);
                break;
        }
        
        // Sync to real
        if (control.Tag is Control real)
        {
            real.Width = control.Width;
            real.Height = control.Height;
            Canvas.SetLeft(real, Canvas.GetLeft(control));
            Canvas.SetTop(real, Canvas.GetTop(control));
        }
    }
}
