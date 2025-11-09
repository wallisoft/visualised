using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VB;

public partial class MainWindow : Window
{
    public bool ShowDefaultMenu { get; set; } = true;
    public string MenuTemplate { get; set; } = "VisualisedFormEditor";
    public string ContextMenuTemplate { get; set; } = "VisualisedEditor";
    public bool EnableDesigner { get; set; } = true;
    public string CurrentProjectPath { get; set; } = "";
    
    private Menu? currentMenu;
    private ContextMenu? currentContextMenu;
    private bool isPreviewMode = false;
    private VbApiServer? apiServer;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize property store
        PropertyStore.Initialize();
        
        // Initialize property store
        PropertyStore.Initialize();
        ProjectManager.Initialize();
        
        // Initialize API control handler
        apiServer = new VbApiServer(this);
        apiServer.Start();
    }
    
    public void ApplyMenuTemplate()
    {
        currentMenu = MenuTemplate switch
        {
            "Basic" => MenuTemplates.CreateBasicMenu(this),
            "VisualisedFormEditor" => MenuTemplates.CreateVisualisedFormEditorMenu(this),
            "SimpleTextEditor" => MenuTemplates.CreateSimpleTextEditorMenu(this),
            _ => MenuTemplates.CreateBasicMenu(this)
        };
        Console.WriteLine($"[MENU] Applied: {MenuTemplate}");
    }
    
    public void ApplyContextMenuTemplate()
    {
        currentContextMenu = ContextMenuTemplate switch
        {
            "Basic" => MenuTemplates.CreateBasicContextMenu(this),
            "VisualisedEditor" => MenuTemplates.CreateVisualisedEditorContextMenu(this),
            "TextEditor" => MenuTemplates.CreateTextEditorContextMenu(this),
            _ => MenuTemplates.CreateBasicContextMenu(this)
        };
        Console.WriteLine($"[CONTEXT] Applied: {ContextMenuTemplate}");
    }
    
    // FILE
    public void HandleNew(object? s, RoutedEventArgs e)
    {
        Console.WriteLine("[FILE] New Project");
    }
    
    public async void HandleOpen(object? s, RoutedEventArgs e)
    {
        var storage = StorageProvider;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Visualised Project",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("VML Files") { Patterns = new[] { "*.vml" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });
        
        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            OpenProject(path);
        }
    }
    
    public void OpenProject(string path)
    {
        Console.WriteLine($"[FILE] Opening: {path}");
        CurrentProjectPath = path;
        
        var name = Path.GetFileNameWithoutExtension(path);
        ProjectManager.AddProject(name, path);
        
        DesignerWindow.LoadVmlIntoCanvas(this, path);
    }
    
    public void HandleSave(object? s, RoutedEventArgs e) => Console.WriteLine("[FILE] Save");
    public void HandleSaveAs(object? s, RoutedEventArgs e) => Console.WriteLine("[FILE] Save As");
    public void HandleExport(object? s, RoutedEventArgs e) => Console.WriteLine("[FILE] Export");
    public void HandleRecent(object? s, RoutedEventArgs e) => Console.WriteLine("[FILE] Recent");
    public void HandlePrint(object? s, RoutedEventArgs e) => Console.WriteLine("[FILE] Print");
    public void HandleExit(object? s, RoutedEventArgs e) => Close();
    
    // EDIT
    public void HandleUndo(object? s, RoutedEventArgs e) => Console.WriteLine("[EDIT] Undo");
    public void HandleRedo(object? s, RoutedEventArgs e) => Console.WriteLine("[EDIT] Redo");
    public void HandleCut(object? s, RoutedEventArgs e) => Console.WriteLine("[EDIT] Cut");
    public void HandleCopy(object? s, RoutedEventArgs e) => Console.WriteLine("[EDIT] Copy");
    public void HandlePaste(object? s, RoutedEventArgs e) => Console.WriteLine("[EDIT] Paste");
    public void HandleDelete(object? s, RoutedEventArgs e) => Console.WriteLine("[EDIT] Delete");
    public void HandleSelectAll(object? s, RoutedEventArgs e) => Console.WriteLine("[EDIT] Select All");
    public void HandleFind(object? s, RoutedEventArgs e) => Console.WriteLine("[EDIT] Find");
    public void HandleReplace(object? s, RoutedEventArgs e) => Console.WriteLine("[EDIT] Replace");
    public void HandleEditVML(object? s, RoutedEventArgs e) => Console.WriteLine("[EDIT] Edit VML Source - F12");
    
    // VIEW
    public void HandleToolbox(object? s, RoutedEventArgs e) 
    { 
        var designer = Content as Control; 
        var toolbox = designer?.FindControl<Border>("toolboxBorder"); 
        if (toolbox != null) toolbox.IsVisible = true; 
    }
    public void HandleProperties(object? s, RoutedEventArgs e) 
    { 
        var designer = Content as Control; 
        var props = designer?.FindControl<Border>("propsBorder"); 
        if (props != null) props.IsVisible = true; 
    }
    public void HandleOutput(object? s, RoutedEventArgs e) => Console.WriteLine("[VIEW] Output");
    public void HandleZoomIn(object? s, RoutedEventArgs e) => Console.WriteLine("[VIEW] Zoom In");
    public void HandleZoomOut(object? s, RoutedEventArgs e) => Console.WriteLine("[VIEW] Zoom Out");
    public void HandleZoomReset(object? s, RoutedEventArgs e) => Console.WriteLine("[VIEW] Zoom Reset");
    public void HandleGridSettings(object? s, RoutedEventArgs e) => Console.WriteLine("[VIEW] Grid Settings");
    
    // FORMAT
    public void HandleFont(object? s, RoutedEventArgs e) => Console.WriteLine("[FORMAT] Font");
    public void HandleWordWrap(object? s, RoutedEventArgs e) => Console.WriteLine("[FORMAT] Word Wrap");
    
    // TOOLS
    public async void HandleMenuEditor(object? s, RoutedEventArgs e)
    {
        var editor = new MenuEditorWindow(this);
        await editor.ShowDialog(this);
    }
    
    public async void HandleOptions(object? s, RoutedEventArgs e)
    {
        var optionsWindow = VmlWindowLoader.LoadWindow("options-window.vml");
        if (optionsWindow != null)
        {
            await optionsWindow.ShowDialog(this);
        }
    }
    
    // SCRIPTEDIT
    public void HandleEditScript(object? s, RoutedEventArgs e) => Console.WriteLine("[SCRIPT] Edit Script - F2");
    
    // PREVIEW
    public void HandleTogglePreview(object? s, RoutedEventArgs e)
    {
        isPreviewMode = !isPreviewMode;
        Console.WriteLine($"[PREVIEW] Toggle - Mode: {(isPreviewMode ? "PREVIEW" : "DESIGN")}");
    }
    
    // CONTEXT
    public void HandleBringToFront(object? s, RoutedEventArgs e) => Console.WriteLine("[CONTEXT] Bring to Front");
    public void HandleSendToBack(object? s, RoutedEventArgs e) => Console.WriteLine("[CONTEXT] Send to Back");
    public void HandleAlign(object? s, RoutedEventArgs e) => Console.WriteLine("[CONTEXT] Align");
    public void HandleSize(object? s, RoutedEventArgs e) => Console.WriteLine("[CONTEXT] Size");
    
    // HELP
    public void HandleDocumentation(object? s, RoutedEventArgs e) => Console.WriteLine("[HELP] Documentation");
    public void HandleVMLReference(object? s, RoutedEventArgs e) => Console.WriteLine("[HELP] VML Reference");
    public void HandleSamples(object? s, RoutedEventArgs e) => Console.WriteLine("[HELP] Samples");
    public void HandleUpdates(object? s, RoutedEventArgs e) => Console.WriteLine("[HELP] Check Updates");
    
    public async void HandleAbout(object? s, RoutedEventArgs e)
    {
        var about = new Window { Title = "About Visualised", Width = 450, Height = 300, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var stack = new StackPanel { Margin = new Avalonia.Thickness(40), Spacing = 15 };
        stack.Children.Add(new TextBlock { Text = "Visualised Designer", FontSize = 24, FontWeight = FontWeight.Bold });
        stack.Children.Add(new TextBlock { Text = "Version 1.0", FontSize = 16 });
        stack.Children.Add(new TextBlock { Text = "Meta-Engineered RAD IDE", FontSize = 14, Margin = new Avalonia.Thickness(0,10,0,0) });
        stack.Children.Add(new TextBlock { Text = "Menu Template Architecture", FontSize = 12, FontStyle = FontStyle.Italic });
        stack.Children.Add(new TextBlock { Text = "Steve Wallis (Wallisoft) & Claude (Anthropic)", FontSize = 12, Margin = new Avalonia.Thickness(0,20,0,0) });
        var btn = new Button { Content = "Close", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Padding = new Avalonia.Thickness(30,8) };
        btn.Click += (ss,ee) => about.Close();
        stack.Children.Add(btn);
        about.Content = stack;
        await about.ShowDialog(this);
    }
    
    public Menu? GetCurrentMenu() => currentMenu;
    public ContextMenu? GetCurrentContextMenu() => currentContextMenu;
}


