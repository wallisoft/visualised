using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VB;

public class DesignerWindow
{
    private static readonly object debugLock = new object();
    private static void DebugLog(string message)
    {
        try
        {
            lock (debugLock)
            {
                File.AppendAllText("./debug.txt", $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
        }
        catch { /* Ignore file errors */ }
    }

    private static Canvas? designCanvas;
    private static PropertiesPanel? propertiesPanel;
    private static Control? selectedControl;
    private static Border? selectionBorder;
    // Multi-selection
    private static HashSet<Control> selectedControls = new HashSet<Control>();
    private static Dictionary<Control, Border> selectionBorders = new Dictionary<Control, Border>();
    private static List<Border> dragGhosts = new List<Border>();
    private static Point dragStartPoint;
    private static Dictionary<Control, Point> dragStartPositions = new Dictionary<Control, Point>();
    private const double DefaultCanvasWidth = 3840;
    private const double DefaultCanvasHeight = 2160;
    private static Border? guideRectangle;
    private static TextBlock? guideLabel;
    private static double overlayWidth = 800;
    private static double overlayHeight = 600;
    private static TextBlock? statusText;
    private static Point dragStart;
    private static bool isDragging;
    private const double EdgeTolerance = 8.0;
    private static string? resizeEdge;
    private static Point resizeStart;
    private static Size originalSize;
    private static Point originalPosition;
    private const double MinControlSize = 20.0;
    private const double GuideOffsetX = 50.0;
    private const double GuideOffsetY = 50.0;
    private static Dictionary<string, int> controlCounters = new Dictionary<string, int>();
    
    // Drag from toolbox
    private static Border? dragGhost;
    private static string? dragControlType;
    private static bool isDraggingFromToolbox;
    private static Popup? ghostPopup;
    
    public static void LoadAndApply(MainWindow window, string vmlPath)
    {
        Console.WriteLine($"[VML] Loading from {vmlPath}");
        
        if (File.Exists(vmlPath))
        {
            var props = ParseVMLProperties(vmlPath);
            ApplyProperties(window, props);
        }
        
        window.ApplyMenuTemplate();
        window.ApplyContextMenuTemplate();
        BuildUI(window);
    }

    public static void LoadVmlIntoCanvas(MainWindow window, string vmlPath)
    {
        Console.WriteLine($"[DESIGNER] Loading VML into canvas: {vmlPath}");
        
        if (!File.Exists(vmlPath))
        {
            Console.WriteLine($"[DESIGNER] File not found: {vmlPath}");
            return;
        }
        
        try
        {
            if (designCanvas != null)
            {
                designCanvas.Children.Clear();
                DebugLog("[DESIGNER] Canvas cleared");
            }
            
            var vmlControls = VmlLoader.Load(vmlPath);
            Console.WriteLine($"[DESIGNER] Loaded {vmlControls.Count} controls from VML");
            
            Control? lastLoadedControl = null;
            if (designCanvas != null)
            {
                foreach (var vmlControl in vmlControls)
                {
                    if (vmlControl.Type == "Window") continue;
                    
                    var dummy = CreateDesignControl(vmlControl.Type);
                    if (dummy == null) continue;
                    
                    dummy.Name = vmlControl.Name ?? vmlControl.Type;
                    DesignProperties.SetIsResizable(dummy, true);
                    DesignProperties.SetIsDraggable(dummy, true);
                    
                    if (vmlControl.Properties.TryGetValue("X", out var xStr) && double.TryParse(xStr, out var x))
                        Canvas.SetLeft(dummy, x);
                    
                    if (vmlControl.Properties.TryGetValue("Y", out var yStr) && double.TryParse(yStr, out var y))
                        Canvas.SetTop(dummy, y);
                    
                    if (vmlControl.Properties.TryGetValue("Width", out var wStr) && double.TryParse(wStr, out var width))
                        dummy.Width = width;
                    
                    if (vmlControl.Properties.TryGetValue("Height", out var hStr) && double.TryParse(hStr, out var height))
                        dummy.Height = height;
                    
                    foreach (var prop in vmlControl.Properties)
                    {
                        if (prop.Key == "X" || prop.Key == "Y" || prop.Key == "Width" || prop.Key == "Height" || prop.Key == "Parent") continue;
                        
                        try
                        {
                            var propInfo = dummy.GetType().GetProperty(prop.Key);
                            if (propInfo != null && propInfo.CanWrite)
                            {
                                var value = Convert.ChangeType(prop.Value, propInfo.PropertyType);
                                propInfo.SetValue(dummy, value);
                            }
                        }
                        catch { }
                    }
                    
                    designCanvas.Children.Add(dummy);
                    MakeDraggableWithCursors(dummy);
                    PropertyStore.SyncControl(dummy);
                    MakeDraggableWithCursors(dummy);
                    PropertyStore.SyncControl(dummy);
                    if (vmlControls.IndexOf(vmlControl) == vmlControls.Count - 1) SelectControl(dummy);
                    DebugLog($"[DESIGNER] Added {vmlControl.Type} '{vmlControl.Name}' at ({Canvas.GetLeft(dummy)},{Canvas.GetTop(dummy)}) {dummy.Width}x{dummy.Height}");
                    lastLoadedControl = dummy;
                }
                if (lastLoadedControl != null) SelectControl(lastLoadedControl);
                
                // Resize overlay if VML specifies Window dimensions
                var windowControl = vmlControls.FirstOrDefault(c => c.Type == "Window");
                if (windowControl != null)
                {
                    double width = 800, height = 600;
                    if (windowControl.Properties.TryGetValue("Width", out var wStr) && double.TryParse(wStr, out var w))
                        width = w;
                    if (windowControl.Properties.TryGetValue("Height", out var hStr) && double.TryParse(hStr, out var h))
                        height = h;
                    UpdateOverlaySize(width, height);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DESIGNER] Error: {ex.Message}");
        }
    }
    
    private static Dictionary<string, string> ParseVMLProperties(string vmlPath)
    {
        var props = new Dictionary<string, string>();
        foreach (var line in File.ReadAllLines(vmlPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
                props[parts[0].Trim()] = parts[1].Trim();
        }
        return props;
    }
    
    private static void ApplyProperties(MainWindow window, Dictionary<string, string> props)
    {
        if (props.TryGetValue("Title", out var title)) window.Title = title;
        if (props.TryGetValue("Width", out var w) && int.TryParse(w, out var width)) window.Width = width;
        if (props.TryGetValue("Height", out var h) && int.TryParse(h, out var height)) window.Height = height;
        if (props.TryGetValue("MenuTemplate", out var mt)) window.MenuTemplate = mt;
        if (props.TryGetValue("ContextMenuTemplate", out var ct)) window.ContextMenuTemplate = ct;
    }
    
    private static void UpdateOverlaySize(double width, double height)
    {
        overlayWidth = width;
        overlayHeight = height;
        if (guideRectangle != null)
        {
            guideRectangle.Width = width;
            guideRectangle.Height = height;
        }
        if (guideLabel != null)
        {
            guideLabel.Text = $"{width:F0}x{height:F0} Design Area";
        }
        Console.WriteLine($"[OVERLAY] Resized to {width}x{height}");
    }

    private static void BuildUI(MainWindow window)
    {
        var root = new DockPanel();
        
        var menu = window.GetCurrentMenu();
        if (menu != null)
        {
            DockPanel.SetDock(menu, Dock.Top);
            root.Children.Add(menu);
        }
        
        statusText = new TextBlock 
        { 
            Text = "Ready",
            Foreground = Brushes.White,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        
        var statusBar = new Border 
        { 
            Background = new SolidColorBrush(Color.Parse("#1b5e20")),
            Height = 30,
            Padding = new Avalonia.Thickness(10, 5),
            Child = statusText
        };
        DockPanel.SetDock(statusBar, Dock.Bottom);
        root.Children.Add(statusBar);
        
        var workspace = new Grid();
        workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        
        var toolbox = BuildToolbox();
        Grid.SetColumn(toolbox, 0);
        workspace.Children.Add(toolbox);
        
        designCanvas = new Canvas 
        { 
            Width = DefaultCanvasWidth,
            Height = DefaultCanvasHeight,
            Background = new SolidColorBrush(Color.Parse("#f1f8e9"))
        };
        
        designCanvas.PointerMoved += (s, e) =>
        {
            if (designCanvas == null || statusText == null) return;
            var pos = e.GetPosition(designCanvas);
            var overlayX = pos.X - GuideOffsetX;
            var overlayY = pos.Y - GuideOffsetY;
            
            var controlName = selectedControl?.Name ?? "None";
            var controlType = selectedControl?.GetType().Name.Replace("Design", "") ?? "";
            var display = controlName != "None" ? $"{controlName} ({controlType})" : "None";
            statusText.Text = $"{display} | Win: {window.ClientSize.Width:F0}x{window.ClientSize.Height:F0} | Canvas: {pos.X:F0},{pos.Y:F0} | Overlay: {overlayX:F0},{overlayY:F0}";
            
            // Update ghost position if dragging from toolbox
            if (isDraggingFromToolbox)
            {
                Console.WriteLine($"[DRAG DEBUG] Canvas moved: isDragging={isDraggingFromToolbox}, ghost={dragGhost != null}, popup={ghostPopup != null}");
                
                if (dragGhost != null && ghostPopup != null)
                {
                    var screenPos = designCanvas.PointToScreen(pos);
                    ghostPopup.HorizontalOffset = screenPos.X - 40;
                    ghostPopup.VerticalOffset = screenPos.Y - 15;
                    Console.WriteLine($"[DRAG DEBUG] Ghost moved to: {screenPos.X - 40:F0},{screenPos.Y - 15:F0}");
                }
                else
                {
                    Console.WriteLine($"[DRAG DEBUG] Ghost or popup is NULL!");
                }
            }
        };
        
        designCanvas.PointerReleased += (s, e) =>
        {
            if (isDraggingFromToolbox && dragControlType != null)
            {
                var pos = e.GetPosition(designCanvas);
                CreateControlAtPosition(dragControlType, pos.X, pos.Y);
                EndToolboxDrag();
            }
        };
        
        guideRectangle = new Border
        {
            Width = 800,
            Height = 600,
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Avalonia.Thickness(2),
            Background = new SolidColorBrush(Color.FromArgb(10, 102, 187, 106)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(guideRectangle, GuideOffsetX);
        Canvas.SetTop(guideRectangle, GuideOffsetY);
        designCanvas.Children.Add(guideRectangle);
        
        guideLabel = new TextBlock
        {
            Text = "800x600 Design Area",
            Foreground = new SolidColorBrush(Color.Parse("#66bb6a")),
            FontWeight = FontWeight.Bold,
            FontSize = 12,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(guideLabel, 55);
        Canvas.SetTop(guideLabel, 55);
        designCanvas.Children.Add(guideLabel);
        
        selectionBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
            BorderThickness = new Avalonia.Thickness(2),
            IsVisible = false,
            IsHitTestVisible = false
        };
        designCanvas.Children.Add(selectionBorder);
        
        designCanvas.PointerPressed += (s, e) =>
        {
            if (e.GetCurrentPoint(designCanvas).Properties.IsLeftButtonPressed)
            {
                selectedControl = null;
                UpdateSelectionBorder();
                UpdateStatusBar(window);
            }
        };
        
        var canvasScroll = new ScrollViewer 
        { 
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        canvasScroll.Content = designCanvas;
        
        Grid.SetColumn(canvasScroll, 1);
        workspace.Children.Add(canvasScroll);
        
        var propsBorder = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#e8f5e9")),
            BorderBrush = new SolidColorBrush(Color.Parse("#ccc")),
            BorderThickness = new Avalonia.Thickness(1, 0, 0, 0)
        };
        
        var propsScroll = new ScrollViewer 
        { 
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Padding = new Avalonia.Thickness(10)
        };
        
        var propsStack = new StackPanel { Spacing = 5 };
        propsStack.Children.Add(new TextBlock 
        { 
            Text = "Properties",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        });
        propsStack.Children.Add(new TextBlock 
        { 
            Text = "Drag from toolbox or click!",
            FontStyle = FontStyle.Italic,
            Foreground = new SolidColorBrush(Color.Parse("#666"))
        });
        
        propsScroll.Content = propsStack;
        propsBorder.Child = propsScroll;
        
        propertiesPanel = new PropertiesPanel(propsStack);
        
        Grid.SetColumn(propsBorder, 2);
        workspace.Children.Add(propsBorder);
        
        root.Children.Add(workspace);
        window.Content = root;
        
        // Update status bar on window resize
        window.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "ClientSize")
            {
                UpdateStatusBar(window);
            }
        };
        
        UpdateStatusBar(window);
        Console.WriteLine("[UI] Designer ready with drag-drop!");
    }
    
        private static void StartToolboxDrag(string controlType, Point startPos)
    {
        Console.WriteLine($"[DRAG DEBUG] ========================================");
        Console.WriteLine($"[DRAG DEBUG] StartToolboxDrag called for {controlType}");
        Console.WriteLine($"[DRAG DEBUG] Start position: {startPos.X:F0},{startPos.Y:F0}");
        
        isDraggingFromToolbox = true;
        dragControlType = controlType;
        
        // Create ghost
        dragGhost = new Border
        {
            Width = 80,
            Height = 30,
            BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
            BorderThickness = new Avalonia.Thickness(2),
            Background = new SolidColorBrush(Color.FromArgb(128, 33, 150, 243)),
            CornerRadius = new CornerRadius(4),
            Child = new TextBlock
            {
                Text = controlType,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            }
        };
        
        Console.WriteLine($"[DRAG DEBUG] Ghost created: {dragGhost != null}");
        
        // Create popup for ghost
        ghostPopup = new Popup
        {
            Child = dragGhost,
            IsOpen = true,
            PlacementMode = PlacementMode.Pointer,
            IsLightDismissEnabled = false
        };
        
        Console.WriteLine($"[DRAG DEBUG] Popup created and opened: {ghostPopup.IsOpen}");
        Console.WriteLine($"[DRAG DEBUG] ========================================");
    }
    
        private static void EndToolboxDrag()
    {
        Console.WriteLine($"[DRAG DEBUG] ========================================");
        Console.WriteLine($"[DRAG DEBUG] EndToolboxDrag called");
        Console.WriteLine($"[DRAG DEBUG] isDraggingFromToolbox: {isDraggingFromToolbox}");
        Console.WriteLine($"[DRAG DEBUG] ghostPopup != null: {ghostPopup != null}");
        
        isDraggingFromToolbox = false;
        dragControlType = null;
        
        if (ghostPopup != null)
        {
            Console.WriteLine($"[DRAG DEBUG] Closing popup");
            ghostPopup.IsOpen = false;
            ghostPopup = null;
        }
        
        dragGhost = null;
        Console.WriteLine($"[DRAG DEBUG] Drag ended and cleaned up");
        Console.WriteLine($"[DRAG DEBUG] ========================================");
    }
    
    private static void CreateControlAtPosition(string controlType, double x, double y)
    {
        if (designCanvas == null) return;
        
        var control = CreateDesignControl(controlType);
        if (control == null) return;
        
        DesignProperties.SetIsResizable(control, true);
        DesignProperties.SetIsDraggable(control, true);
        
        // Auto-generate name
        if (!controlCounters.ContainsKey(controlType))
            controlCounters[controlType] = 0;
        
        controlCounters[controlType]++;
        var controlName = $"{controlType}_{controlCounters[controlType]}";
        control.Name = controlName;
        
        DebugLog($"[CONTROL] Named: {controlName}");
        
        // Place at mouse position
        Canvas.SetLeft(control, Math.Max(0, x - 40));
        Canvas.SetTop(control, Math.Max(0, y - 15));
        
        MakeDraggableWithCursors(control);
        designCanvas.Children.Add(control);
        SelectControl(control);
        PropertyStore.SyncControl(control);
        
        DebugLog($"[ADD] {controlType} at {x:F0},{y:F0}");
    }
    
    private static void UpdateStatusBar(MainWindow window)
    {
        if (statusText == null) return;
        var controlName = selectedControl?.Name ?? "None";
        var controlType = selectedControl?.GetType().Name.Replace("Design", "") ?? "";
        var display = controlName != "None" ? $"{controlName} ({controlType})" : "None";
        statusText.Text = $"{display} | Win: {window.ClientSize.Width:F0}x{window.ClientSize.Height:F0}";
    }
    
    private static Border BuildToolbox()
    {
        var toolboxBorder = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#e8f5e9")),
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Avalonia.Thickness(0, 0, 1, 0)
        };
        
        var toolboxStack = new StackPanel { Margin = new Avalonia.Thickness(10), Spacing = 5 };
        
        toolboxStack.Children.Add(new TextBlock 
        { 
            Text = "Toolbox",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        });
        
        var common = new Expander { Header = "Common", IsExpanded = true, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        var commonStack = new StackPanel { Spacing = 3 };
        commonStack.Children.Add(CreateToolboxButton("Button"));
        commonStack.Children.Add(CreateToolboxButton("TextBox"));
        commonStack.Children.Add(CreateToolboxButton("TextBlock"));
        commonStack.Children.Add(CreateToolboxButton("CheckBox"));
        commonStack.Children.Add(CreateToolboxButton("ListBox"));
        commonStack.Children.Add(CreateToolboxButton("Label"));
        commonStack.Children.Add(CreateToolboxButton("Separator"));
        common.Content = commonStack;
        toolboxStack.Children.Add(common);
        
        var input = new Expander { Header = "Input", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        var inputStack = new StackPanel { Spacing = 3 };
        inputStack.Children.Add(CreateToolboxButton("ComboBox"));
        inputStack.Children.Add(CreateToolboxButton("ListBox"));
        inputStack.Children.Add(CreateToolboxButton("RadioButton"));
        input.Content = inputStack;
        toolboxStack.Children.Add(input);
        
        var layout = new Expander { Header = "Layout", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        var layoutStack = new StackPanel { Spacing = 3 };
        layoutStack.Children.Add(CreateToolboxButton("StackPanel"));
        layoutStack.Children.Add(CreateToolboxButton("Grid"));
        layoutStack.Children.Add(CreateToolboxButton("Border"));
        layout.Content = layoutStack;
        toolboxStack.Children.Add(layout);
        
        toolboxBorder.Child = new ScrollViewer { Content = toolboxStack };
        return toolboxBorder;
    }
    
                                    private static Button CreateToolboxButton(string controlType)
    {
        var btn = new Button 
        { 
            Content = controlType,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            Padding = new Avalonia.Thickness(8, 4),
            FontSize = 12,
            Cursor = new Cursor(StandardCursorType.Hand),
            Background = new SolidColorBrush(Color.Parse("#e8f5e9")),
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Avalonia.Thickness(3),
            CornerRadius = new CornerRadius(4)
        };
        
        btn.GotFocus += (s, e) =>
        {
            btn.BorderBrush = new SolidColorBrush(Color.Parse("#2196F3"));
            Console.WriteLine($"[FOCUS] {controlType} got focus");
        };
        
        btn.LostFocus += (s, e) =>
        {
            btn.BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a"));
            Console.WriteLine($"[FOCUS] {controlType} lost focus");
        };
        
        // Simple click - works reliably
        btn.Click += (s, e) =>
        {
            Console.WriteLine($"[CLICK] {controlType}");
            btn.BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a"));
            btn.Focus(NavigationMethod.Unspecified);
            AddControlToCanvas(controlType);
        };
        
        return btn;
    }
    
    private static void AddControlToCanvas(string controlType)
    {
        if (designCanvas == null) return;
        
        var control = CreateDesignControl(controlType);
        if (control == null) return;
        
        DesignProperties.SetIsResizable(control, true);
        DesignProperties.SetIsDraggable(control, true);
        
        // Auto-generate name
        if (!controlCounters.ContainsKey(controlType))
            controlCounters[controlType] = 0;
        
        controlCounters[controlType]++;
        var controlName = $"{controlType}_{controlCounters[controlType]}";
        control.Name = controlName;
        
        DebugLog($"[CONTROL] Named: {controlName}");
        
        var baseX = 150.0;
        var baseY = 150.0;
        var offset = 0;
        
        while (true)
        {
            var testX = baseX + (offset * 20);
            var testY = baseY + (offset * 20);
            
            var occupied = GetDesignControls().Any(c =>
            {
                var cx = Canvas.GetLeft(c);
                var cy = Canvas.GetTop(c);
                return Math.Abs(cx - testX) < 5 && Math.Abs(cy - testY) < 5;
            });
            
            if (!occupied)
            {
                Canvas.SetLeft(control, testX);
                Canvas.SetTop(control, testY);
                MakeDraggableWithCursors(control);
                designCanvas.Children.Add(control);
                SelectControl(control);
                PropertyStore.SyncControl(control);
                DebugLog($"[ADD] {controlType} → {control.Name}");
                return;
            }
            
            offset++;
            if (offset > 100) break;
        }
    }
    
            private static void MakeDraggableWithCursors(Control control)
    {
        Console.WriteLine($"[SETUP] MakeDraggableWithCursors for {control.Name}");
        
        var contextMenu = new ContextMenu();
        
        var editScript = new MenuItem { Header = "Edit Script (F2)" };
        editScript.Click += (s, e) => OpenScriptEditor(control);
        
        var delete = new MenuItem { Header = "Delete" };
        delete.Click += (s, e) =>
        {
            if (designCanvas != null)
            {
                designCanvas.Children.Remove(control);
                if (selectedControl == control)
                {
                    selectedControl = null;
                    UpdateSelectionBorder();
                }
                Console.WriteLine($"[DELETE] {control.Name}");
            }
        };
        
        var bringToFront = new MenuItem { Header = "Bring to Front" };
        bringToFront.Click += (s, e) =>
        {
            if (designCanvas != null)
            {
                designCanvas.Children.Remove(control);
                designCanvas.Children.Add(control);
                UpdateSelectionBorder();
                Console.WriteLine($"[Z-ORDER] Brought to front ({control.Name})");
            }
        };
        
        var sendToBack = new MenuItem { Header = "Send to Back" };
        sendToBack.Click += (s, e) =>
        {
            if (designCanvas != null)
            {
                designCanvas.Children.Remove(control);
                designCanvas.Children.Insert(0, control);
                UpdateSelectionBorder();
                Console.WriteLine($"[Z-ORDER] Sent to back ({control.Name})");
            }
        };
        
        contextMenu.Items.Add(editScript);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(delete);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(bringToFront);
        contextMenu.Items.Add(sendToBack);
        
        control.ContextMenu = contextMenu;
        
        // Cursor feedback - THIS WORKS
        control.PointerMoved += (s, e) =>
        {
            if (selectedControl != control) return;
            
            var pos = e.GetPosition(control);
            var zone = GetEdgeZone(pos, control);
            
            Console.WriteLine($"[CURSOR] {control.Name} zone={zone ?? "center"} at {pos.X:F0},{pos.Y:F0}");
            
            control.Cursor = zone switch
            {
                "E" or "W" => new Cursor(StandardCursorType.SizeWestEast),
                "N" or "S" => new Cursor(StandardCursorType.SizeNorthSouth),
                "NE" or "SW" => new Cursor(StandardCursorType.TopRightCorner),
                "NW" or "SE" => new Cursor(StandardCursorType.TopLeftCorner),
                _ => new Cursor(StandardCursorType.SizeAll)
            };
        };
        
        // Press - START HERE
        control.PointerPressed += (s, e) =>
        {
            Console.WriteLine($"[PRESSED] {control.Name} PointerPressed fired!");
            Console.WriteLine($"[PRESSED]   IsLeftButton: {e.GetCurrentPoint(control).Properties.IsLeftButtonPressed}");
            Console.WriteLine($"[PRESSED]   designCanvas null? {designCanvas == null}");
            
            if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed || designCanvas == null)
            {
                Console.WriteLine($"[PRESSED]   ABORTED - not left button or no canvas");
                return;
            }
            
            var pos = e.GetPosition(control);
            var zone = GetEdgeZone(pos, control);
            
            Console.WriteLine($"[PRESSED]   Position: {pos.X:F0},{pos.Y:F0}");
            Console.WriteLine($"[PRESSED]   Zone: {zone ?? "center"}");
            
            if (zone != null)
            {
                // RESIZE
                resizeEdge = zone;
                resizeStart = e.GetPosition(designCanvas);
                originalSize = new Size(control.Width, control.Height);
                originalPosition = new Point(Canvas.GetLeft(control), Canvas.GetTop(control));
                e.Pointer.Capture(control);
                
                Console.WriteLine($"[RESIZE START] Edge={resizeEdge}");
                Console.WriteLine($"[RESIZE START]   Original size: {originalSize.Width:F0}x{originalSize.Height:F0}");
                Console.WriteLine($"[RESIZE START]   Original pos: {originalPosition.X:F0},{originalPosition.Y:F0}");
                Console.WriteLine($"[RESIZE START]   Pointer captured!");
            }
            else
            {
                // MOVE
                dragStart = e.GetPosition(designCanvas);
                isDragging = true;
                e.Pointer.Capture(control);
                
                Console.WriteLine($"[MOVE START] Dragging={isDragging}");
                Console.WriteLine($"[MOVE START]   Start pos: {dragStart.X:F0},{dragStart.Y:F0}");
                Console.WriteLine($"[MOVE START]   Pointer captured!");
            }
            
            SelectControl(control);
            e.Handled = true;
            
            Console.WriteLine($"[PRESSED]   Event handled and control selected");
        };
        
        // Move - RESIZE OR DRAG
        control.PointerMoved += (s, e) =>
        {
            if (designCanvas == null || selectedControl != control) return;
            
            var currentPos = e.GetPosition(designCanvas);
            
            Console.WriteLine($"[MOVED] {control.Name} resizeEdge={resizeEdge ?? "null"} isDragging={isDragging}");
            
            if (resizeEdge != null)
            {
                // RESIZE MODE
                var deltaX = currentPos.X - resizeStart.X;
                var deltaY = currentPos.Y - resizeStart.Y;
                
                Console.WriteLine($"[RESIZE] Delta: {deltaX:F0},{deltaY:F0}");
                
                var newWidth = originalSize.Width;
                var newHeight = originalSize.Height;
                var newLeft = originalPosition.X;
                var newTop = originalPosition.Y;
                
                switch (resizeEdge)
                {
                    case "E":
                        newWidth = Math.Max(MinControlSize, originalSize.Width + deltaX);
                        break;
                    case "W":
                        newWidth = Math.Max(MinControlSize, originalSize.Width - deltaX);
                        newLeft = originalPosition.X + (originalSize.Width - newWidth);
                        break;
                    case "S":
                        newHeight = Math.Max(MinControlSize, originalSize.Height + deltaY);
                        break;
                    case "N":
                        newHeight = Math.Max(MinControlSize, originalSize.Height - deltaY);
                        newTop = originalPosition.Y + (originalSize.Height - newHeight);
                        break;
                    case "SE":
                        newWidth = Math.Max(MinControlSize, originalSize.Width + deltaX);
                        newHeight = Math.Max(MinControlSize, originalSize.Height + deltaY);
                        break;
                    case "SW":
                        newWidth = Math.Max(MinControlSize, originalSize.Width - deltaX);
                        newLeft = originalPosition.X + (originalSize.Width - newWidth);
                        newHeight = Math.Max(MinControlSize, originalSize.Height + deltaY);
                        break;
                    case "NE":
                        newWidth = Math.Max(MinControlSize, originalSize.Width + deltaX);
                        newHeight = Math.Max(MinControlSize, originalSize.Height - deltaY);
                        newTop = originalPosition.Y + (originalSize.Height - newHeight);
                        break;
                    case "NW":
                        newWidth = Math.Max(MinControlSize, originalSize.Width - deltaX);
                        newLeft = originalPosition.X + (originalSize.Width - newWidth);
                        newHeight = Math.Max(MinControlSize, originalSize.Height - deltaY);
                        newTop = originalPosition.Y + (originalSize.Height - newHeight);
                        break;
                }
                
                Console.WriteLine($"[RESIZE] New size: {newWidth:F0}x{newHeight:F0}");
                
                control.Width = newWidth;
                control.Height = newHeight;
                Canvas.SetLeft(control, Math.Max(0, newLeft));
                Canvas.SetTop(control, Math.Max(0, newTop));
                
                UpdateSelectionBorder();
            }
            else if (isDragging)
            {
                // MOVE MODE
                var deltaX = currentPos.X - dragStart.X;
                var deltaY = currentPos.Y - dragStart.Y;
                
                Console.WriteLine($"[MOVE] Delta: {deltaX:F0},{deltaY:F0}");
                
                var left = Canvas.GetLeft(control) + deltaX;
                var top = Canvas.GetTop(control) + deltaY;
                
                Canvas.SetLeft(control, Math.Max(0, left));
                Canvas.SetTop(control, Math.Max(0, top));
                
                UpdateSelectionBorder();
                dragStart = currentPos;
            }
        };
        
        // Release
        control.PointerReleased += (s, e) =>
        {
            Console.WriteLine($"[RELEASED] {control.Name} resizeEdge={resizeEdge ?? "null"} isDragging={isDragging}");
            
            isDragging = false;
            resizeEdge = null;
            e.Pointer.Capture(null);
            
            Console.WriteLine($"[RELEASED]   Cleared and released pointer");
        };
        
        control.PointerCaptureLost += (s, e) =>
        {
            Console.WriteLine($"[CAPTURE LOST] {control.Name}");
            isDragging = false;
            resizeEdge = null;
        };
    }
    
    private static void OpenScriptEditor(Control control)
    {
        var controlName = control.GetType().Name.Replace("Design", "");
        
        var vmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "visual-script-editor.vml");
        
        var context = new Dictionary<string, object>
        {
            { "controlName", controlName },
            { "control", control },
            { "existingScript", DesignProperties.GetScript(control) }
        };
        
        var window = VmlWindowLoader.LoadWindow(vmlPath);
        if (window != null)
        {
            window.Show();
            Console.WriteLine($"[SCRIPT EDITOR] Opened VML window for {controlName}");
        }
        else
        {
            Console.WriteLine($"[SCRIPT EDITOR] Failed to load VML");
        }
    }
    
    private static string? GetEdgeZone(Point pos, Control control)
    {
        var width = control.Bounds.Width;
        var height = control.Bounds.Height;
        
        bool nearLeft = pos.X < EdgeTolerance;
        bool nearRight = pos.X > width - EdgeTolerance;
        bool nearTop = pos.Y < EdgeTolerance;
        bool nearBottom = pos.Y > height - EdgeTolerance;
        
        if (nearTop && nearLeft) return "NW";
        if (nearTop && nearRight) return "NE";
        if (nearBottom && nearLeft) return "SW";
        if (nearBottom && nearRight) return "SE";
        
        if (nearTop) return "N";
        if (nearBottom) return "S";
        if (nearLeft) return "W";
        if (nearRight) return "E";
        
        return null;
    }
    
    private static void ClearSelection()
    {
        foreach (var border in selectionBorders.Values)
        {
            if (designCanvas != null && designCanvas.Children.Contains(border))
                designCanvas.Children.Remove(border);
        }
        selectionBorders.Clear();
        selectedControls.Clear();
    }
    
    private static void CreateSelectionBorder(Control control)
    {
        if (selectionBorders.ContainsKey(control)) return;
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
            BorderThickness = new Avalonia.Thickness(2),
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            ZIndex = 999
        };
        UpdateControlBorder(control, border);
        selectionBorders[control] = border;
        if (designCanvas != null)
            designCanvas.Children.Add(border);
    }
    
    private static void UpdateControlBorder(Control control, Border border)
    {
        var left = Canvas.GetLeft(control);
        var top = Canvas.GetTop(control);
        Canvas.SetLeft(border, left - 2);
        Canvas.SetTop(border, top - 2);
        border.Width = control.Bounds.Width + 4;
        border.Height = control.Bounds.Height + 4;
        border.IsVisible = true;
    }
    
    private static void UpdateAllSelectionBorders()
    {
        foreach (var kvp in selectionBorders)
            UpdateControlBorder(kvp.Key, kvp.Value);
    }
    
    private static void CreateDragGhosts()
    {
        ClearDragGhosts();
        dragStartPositions.Clear();
        foreach (var control in selectedControls)
        {
            var ghost = new Border
            {
                Width = control.Bounds.Width,
                Height = control.Bounds.Height,
                BorderBrush = new SolidColorBrush(Color.Parse("#4CAF50")),
                BorderThickness = new Avalonia.Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(30, 76, 175, 80)),
                IsHitTestVisible = false,
                ZIndex = 1000
            };
            var left = Canvas.GetLeft(control);
            var top = Canvas.GetTop(control);
            dragStartPositions[control] = new Point(left, top);
            Canvas.SetLeft(ghost, left);
            Canvas.SetTop(ghost, top);
            dragGhosts.Add(ghost);
            if (designCanvas != null)
                designCanvas.Children.Add(ghost);
        }
        DebugLog($"[GHOST] Created {dragGhosts.Count} drag ghosts");
    }
    
    private static void UpdateDragGhosts(double deltaX, double deltaY)
    {
        int i = 0;
        foreach (var control in selectedControls)
        {
            if (i < dragGhosts.Count && dragStartPositions.ContainsKey(control))
            {
                Canvas.SetLeft(dragGhosts[i], dragStartPositions[control].X + deltaX);
                Canvas.SetTop(dragGhosts[i], dragStartPositions[control].Y + deltaY);
            }
            i++;
        }
    }
    
    private static void ApplyDragToSelection(double deltaX, double deltaY)
    {
        foreach (var control in selectedControls)
        {
            if (dragStartPositions.ContainsKey(control))
            {
                Canvas.SetLeft(control, dragStartPositions[control].X + deltaX);
                Canvas.SetTop(control, dragStartPositions[control].Y + deltaY);
            }
        }
        UpdateAllSelectionBorders();
    }
    
    private static void ClearDragGhosts()
    {
        foreach (var ghost in dragGhosts)
        {
            if (designCanvas != null && designCanvas.Children.Contains(ghost))
                designCanvas.Children.Remove(ghost);
        }
        dragGhosts.Clear();
    }

    private static void SelectControl(Control control)
    {
        selectedControl = control;
        propertiesPanel?.ShowPropertiesFor(control);
        UpdateSelectionBorder();
        Console.WriteLine($"[SELECT] {control.Name ?? control.GetType().Name}");
    }
    
            private static void UpdateSelectionBorder()
    {
        if (selectedControl == null || selectionBorder == null)
        {
            if (selectionBorder != null) selectionBorder.IsVisible = false;
            return;
        }
        
        var left = Canvas.GetLeft(selectedControl);
        var top = Canvas.GetTop(selectedControl);
        var width = selectedControl.Bounds.Width;
        var height = selectedControl.Bounds.Height;
        
        Canvas.SetLeft(selectionBorder, left - 2);
        Canvas.SetTop(selectionBorder, top - 2);
        selectionBorder.Width = width + 4;
        selectionBorder.Height = height + 4;
        selectionBorder.IsVisible = true;
        
        // CRITICAL: Only bring to top if NOT currently dragging/resizing!
        // Moving control during pointer capture breaks the capture!
        if (designCanvas != null && !isDragging && resizeEdge == null)
        {
            designCanvas.Children.Remove(selectedControl);
            designCanvas.Children.Add(selectedControl);
            designCanvas.Children.Remove(selectionBorder);
            designCanvas.Children.Add(selectionBorder);
            Console.WriteLine($"[Z-ORDER] Control and border brought to top ({selectedControl.Name})");
        }
    }
    
    private static List<Control> GetDesignControls()
    {
        if (designCanvas == null) return new List<Control>();
        
        var controls = new List<Control>();
        foreach (var child in designCanvas.Children)
        {
            if (child == selectionBorder || child == guideRectangle) continue;
            if (child is TextBlock) continue;
            if (child is Control c)
                controls.Add(c);
        }
        return controls;
    }
    
    private static Control? CreateDesignControl(string controlType)
    {
        return controlType switch
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
    }
}















