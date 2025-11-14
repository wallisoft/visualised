using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    private static Canvas? designCanvas;
    private static Control? selectedControl;
    private static Rectangle? selectionBorder;
    private static PropertiesPanel? propertiesPanel;
    private static Dictionary<string, int> controlCounters = new();
    private static Rectangle? designOverlay;
    
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
        BuildUI(window, vmlPath);
    }

    // ========================================
    // BUILD MAIN UI
    // ========================================
    // ========================================
// BUILD MAIN UI
// ========================================
private static TextBlock? statusText;

private static void BuildUI(MainWindow window, string vmlPath)
{
    // Main container with menu, workspace, status
    var mainGrid = new Grid
    {
        RowDefinitions = new RowDefinitions("Auto,*,Auto")
    };

    // ========================================
    // ROW 0: MENU BAR (PLACEHOLDER)
    // ========================================
    var menuBar = new Border
    {
        Background = new SolidColorBrush(Color.Parse("#66bb6a")),
        Height = 30,
        Child = new TextBlock 
        { 
            Text = "  Menu (placeholder)", 
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold
        }
    };
    Grid.SetRow(menuBar, 0);
    mainGrid.Children.Add(menuBar);

    // ========================================
    // ROW 1: WORKSPACE (3-COLUMN GRID)
    // ========================================
    var workspace = new Grid
    {
        ColumnDefinitions = new ColumnDefinitions("250,*,0")
    };
    Grid.SetRow(workspace, 1);

    // ========================================
    // COLUMN 0: FORMBUILDER PANEL
    // ========================================
    var formBuilderBorder = new Border
    {
        Background = new SolidColorBrush(Color.Parse("#e8f5e9")),
        BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
        BorderThickness = new Thickness(2),
        CornerRadius = new CornerRadius(2),
        Padding = new Thickness(5),
        Margin = new Thickness(5)
    };
    Grid.SetColumn(formBuilderBorder, 0);
    
    var formBuilderStack = new StackPanel 
    { 
        Margin = new Thickness(5), 
        Spacing = 5 
    };
    
    // Title bar with X button
    var titleBar = new Border
    {
        Background = Brushes.Transparent,
        Padding = new Thickness(5, 3, 5, 3),
        Margin = new Thickness(0, 0, 0, 10)
    };
    
    var titleGrid = new Grid
    {
        ColumnDefinitions = new ColumnDefinitions("*,Auto")
    };
    
    var title = new TextBlock
    {
        Text = "FormBuilder",
        FontSize = 17,
        FontWeight = FontWeight.Bold,
        Foreground = new SolidColorBrush(Color.Parse("#424242")),
        VerticalAlignment = VerticalAlignment.Center
    };
    
    var closeBtn = new Button
    {
        Content = "✕",
        Width = 20,
        Height = 20,
        FontSize = 14,
        FontWeight = FontWeight.Bold,
        Padding = new Thickness(0, -2, 0, 0),
        Background = Brushes.Transparent,
        Foreground = new SolidColorBrush(Color.Parse("#424242")),
        BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
        BorderThickness = new Thickness(2),
        CornerRadius = new CornerRadius(2),
        Cursor = new Cursor(StandardCursorType.Hand)
    };
    
    closeBtn.Click += (s, e) => formBuilderBorder.IsVisible = false;
    
    Grid.SetColumn(title, 0);
    Grid.SetColumn(closeBtn, 1);
    titleGrid.Children.Add(title);
    titleGrid.Children.Add(closeBtn);
    titleBar.Child = titleGrid;
    formBuilderStack.Children.Add(titleBar);
    
    // Control selector
    var selectorRow = new StackPanel 
    { 
        Orientation = Orientation.Horizontal, 
        Spacing = 5,
        Margin = new Thickness(0, 10, 0, 10)
    };
    
    var controlSelector = new TinyCombo();
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
    controlSelector.SelectionChanged += async (s, selected) =>
    {
        if (selected?.ToString() == "MainWindow")
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
            await warning.ShowDialog(window);
            
            controlSelector.Text = "";
            return;
        }
        
        selectedControlType = selected?.ToString() ?? "Button";
    };
    
    selectorRow.Children.Add(controlSelector);
    
    var addBtn = new Button
    {
        Content = "Add",
        Width = 70,
        Height = 28,
        FontSize = 13,
        FontWeight = FontWeight.Bold,
        Background = Brushes.White,
        Foreground = new SolidColorBrush(Color.Parse("#2e7d32")),
        BorderBrush = new SolidColorBrush(Color.Parse("#2e7d32")),
        BorderThickness = new Thickness(2),
        CornerRadius = new CornerRadius(3),
        Cursor = new Cursor(StandardCursorType.Hand)
    };
    
    addBtn.Click += (s, e) => AddControlToCanvas(selectedControlType);
    
    selectorRow.Children.Add(addBtn);
    formBuilderStack.Children.Add(selectorRow);
    
    // Properties panel (scrollable)
    var propsScroll = new ScrollViewer
    {
        VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
        Padding = new Thickness(5)
    };
    
    var propsStack = new StackPanel { Spacing = 5 };
    propsScroll.Content = propsStack;
    formBuilderStack.Children.Add(propsScroll);
    
    propertiesPanel = new PropertiesPanel(propsStack);
    propertiesPanel.PanelCloseRequested += (s, e) => 
    {
        formBuilderBorder.IsVisible = false;
    };
    
    formBuilderBorder.Child = formBuilderStack;
    workspace.Children.Add(formBuilderBorder);

    // ========================================
    // COLUMN 1: CANVAS WITH OVERLAY
    // ========================================
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
        Background = new SolidColorBrush(Color.Parse("#f5f5f5"))
    };
    
    // 800x600 centered overlay, 100px from top
    designOverlay = new Rectangle
    {
        Width = 800,
        Height = 600,
        Fill = new SolidColorBrush(Color.FromArgb(30, 102, 187, 106)),
        Stroke = new SolidColorBrush(Color.Parse("#66bb6a")),
        StrokeThickness = 2,
        IsHitTestVisible = false
    };
    
    // Center horizontally in 4000px canvas: (4000-800)/2 = 1600
    Canvas.SetLeft(designOverlay, 1600);
    Canvas.SetTop(designOverlay, 100);  // 100px from top
    designCanvas.Children.Add(designOverlay);
    
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
    
    mainGrid.Children.Add(workspace);

    // ========================================
    // ROW 2: STATUS BAR
    // ========================================
    var statusBar = new Border
    {
        Background = new SolidColorBrush(Color.Parse("#f0f0f0")),
        BorderBrush = new SolidColorBrush(Color.Parse("#ccc")),
        BorderThickness = new Thickness(0, 1, 0, 0),
        Height = 25
    };
    
    statusText = new TextBlock
    {
        Text = "Ready",
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(10, 0, 0, 0),
        FontSize = 11
    };
    
    statusBar.Child = statusText;
    Grid.SetRow(statusBar, 2);
    mainGrid.Children.Add(statusBar);
    
    // Set window content
    window.Content = mainGrid;
    
    // ========================================
    // LOAD CONTROLS
    // ========================================
    LoadVmlControls(vmlPath);
    LoadPropertyStoreControls();
    
    Console.WriteLine("[UI] Designer ready!");
}

// ========================================
// UPDATE STATUS BAR
// ========================================
private static void UpdateStatusBar()
{
    if (statusText == null) return;
    
    var controlName = selectedControl?.Name ?? "None";
    statusText.Text = $"Selected: {controlName}";
}

    // ========================================
    // LOAD VML CONTROLS
    // ========================================
    private static void LoadVmlControls(string vmlPath)
    {
        Console.WriteLine($"[VML] Loading from {vmlPath}");
        var vmlControls = VmlLoader.Load(vmlPath);
        
        if (designCanvas == null) return;
        
        foreach (var vmlControl in vmlControls)
        {
            if (vmlControl.Type == "Window") continue;
            
            var (dummy, real) = CreateControlPair(vmlControl.Type, vmlControl.Name ?? vmlControl.Type);
            if (dummy == null) continue;
            
            // Apply VML properties
            if (vmlControl.Properties.TryGetValue("X", out var xStr) && double.TryParse(xStr, out var x))
                Canvas.SetLeft(dummy, x);
            
            if (vmlControl.Properties.TryGetValue("Y", out var yStr) && double.TryParse(yStr, out var y))
                Canvas.SetTop(dummy, y);
            
            if (vmlControl.Properties.TryGetValue("Width", out var wStr) && double.TryParse(wStr, out var width))
                dummy.Width = width;
            
            if (vmlControl.Properties.TryGetValue("Height", out var hStr) && double.TryParse(hStr, out var height))
                dummy.Height = height;
            
            // Apply other properties
            foreach (var prop in vmlControl.Properties)
            {
                if (prop.Key == "X" || prop.Key == "Y" || prop.Key == "Width" || prop.Key == "Height") continue;
                
                try
                {
                    var propInfo = real.GetType().GetProperty(prop.Key);
                    if (propInfo != null && propInfo.CanWrite)
                    {
                        var value = Convert.ChangeType(prop.Value, propInfo.PropertyType);
                        propInfo.SetValue(real, value);
                    }
                }
                catch { }
            }
            
            Canvas.SetLeft(real, Canvas.GetLeft(dummy));
            Canvas.SetTop(real, Canvas.GetLeft(dummy));
            
            designCanvas.Children.Add(dummy);
            designCanvas.Children.Add(real);
            MakeDraggable(dummy);
        }
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
    private static (Control dummy, Control real) CreateControlPair(string controlType, string name)
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
            real.Name = name;
            real.IsVisible = false;
            real.IsEnabled = false;
            real.IsHitTestVisible = false;
            dummy.Tag = real;
        }
        
        return (dummy!, real!);
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
        if (dummy == null) return;
        
        // Stack at 200,200 with offset
        var baseX = 200.0;
        var baseY = 200.0;
        var offset = 0;
        
        while (true)
        {
            var testX = baseX + (offset * 20);
            var testY = baseY + (offset * 20);
            
            var occupied = designCanvas.Children.OfType<Control>()
                .Any(c => Math.Abs(Canvas.GetLeft(c) - testX) < 5 && Math.Abs(Canvas.GetTop(c) - testY) < 5);
            
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
                
                return;
            }
            offset++;
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

    // ========================================
    // DRAG & RESIZE
    // ========================================
    private static void MakeDraggable(Control control)
    {
        control.PointerPressed += (s, e) =>
        {
            if (e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
            {
                var pos = e.GetPosition(control);
                var zone = GetResizeZone(control, pos);
                
                if (zone != null)
                {
                    // Start resize
                    resizeEdge = zone;
                    dragStartX = Canvas.GetLeft(control);
                    dragStartY = Canvas.GetTop(control);
                    dragStart = e.GetPosition(designCanvas);
                }
                else
                {
                    // Start move
                    isDragging = true;
                    dragStart = e.GetPosition(designCanvas);
                    dragStartX = Canvas.GetLeft(control);
                    dragStartY = Canvas.GetTop(control);
                }
                
                SelectControl(control);
                e.Handled = true;
            }
        };
        
        control.PointerMoved += (s, e) =>
        {
            if (isDragging)
            {
                // Move
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
            }
            else if (resizeEdge != null)
            {
                // Resize
                var current = e.GetPosition(designCanvas);
                var deltaX = current.X - dragStart.X;
                var deltaY = current.Y - dragStart.Y;
                
                HandleResize(control, resizeEdge, deltaX, deltaY);
                UpdateSelectionBorder();
            }
            else
            {
                // Update cursor
                var pos = e.GetPosition(control);
                var zone = GetResizeZone(control, pos);
                control.Cursor = GetCursorForZone(zone);
            }
        };
        
        control.PointerReleased += (s, e) =>
        {
            if (isDragging || resizeEdge != null)
            {
                // Save position
                if (control.Tag is Control real)
                {
                    PropertyStore.Set(control.Name, "X", Canvas.GetLeft(control).ToString());
                    PropertyStore.Set(control.Name, "Y", Canvas.GetTop(control).ToString());
                    PropertyStore.Set(control.Name, "Width", control.Bounds.Width.ToString());
                    PropertyStore.Set(control.Name, "Height", control.Bounds.Height.ToString());
                }
                
                isDragging = false;
                resizeEdge = null;
            }
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
    
    private static void HandleResize(Control control, string edge, double deltaX, double deltaY)
    {
        var x = Canvas.GetLeft(control);
        var y = Canvas.GetTop(control);
        var w = control.Width;
        var h = control.Height;
        
        switch (edge)
        {
            case "E":
                control.Width = Math.Max(20, w + deltaX);
                break;
            case "W":
                control.Width = Math.Max(20, w - deltaX);
                Canvas.SetLeft(control, x + deltaX);
                break;
            case "S":
                control.Height = Math.Max(20, h + deltaY);
                break;
            case "N":
                control.Height = Math.Max(20, h - deltaY);
                Canvas.SetTop(control, y + deltaY);
                break;
            case "SE":
                control.Width = Math.Max(20, w + deltaX);
                control.Height = Math.Max(20, h + deltaY);
                break;
            case "SW":
                control.Width = Math.Max(20, w - deltaX);
                control.Height = Math.Max(20, h + deltaY);
                Canvas.SetLeft(control, x + deltaX);
                break;
            case "NE":
                control.Width = Math.Max(20, w + deltaX);
                control.Height = Math.Max(20, h - deltaY);
                Canvas.SetTop(control, y + deltaY);
                break;
            case "NW":
                control.Width = Math.Max(20, w - deltaX);
                control.Height = Math.Max(20, h - deltaY);
                Canvas.SetLeft(control, x + deltaX);
                Canvas.SetTop(control, y + deltaY);
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
