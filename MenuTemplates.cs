using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System;

namespace VB;

public static class MenuTemplates
{
    public static Menu CreateBasicMenu(MainWindow window)
    {
        var menu = new Menu { Background = new SolidColorBrush(Color.Parse("#1b5e20")) };
        
        var file = new MenuItem { Header = "File", Foreground = Brushes.White };
        file.Items.Add(CreateItem("New", window.HandleNew, "Ctrl+N"));
        file.Items.Add(CreateItem("Open...", window.HandleOpen, "Ctrl+O"));
        file.Items.Add(CreateItem("Save", window.HandleSave, "Ctrl+S"));
        file.Items.Add(new Separator());
        file.Items.Add(CreateItem("Exit", window.HandleExit, "Alt+F4"));
        
        var edit = new MenuItem { Header = "Edit", Foreground = Brushes.White };
        edit.Items.Add(CreateItem("Undo", window.HandleUndo, "Ctrl+Z"));
        edit.Items.Add(CreateItem("Redo", window.HandleRedo, "Ctrl+Y"));
        edit.Items.Add(new Separator());
        edit.Items.Add(CreateItem("Cut", window.HandleCut, "Ctrl+X"));
        edit.Items.Add(CreateItem("Copy", window.HandleCopy, "Ctrl+C"));
        edit.Items.Add(CreateItem("Paste", window.HandlePaste, "Ctrl+V"));
        
        var help = new MenuItem { Header = "Help", Foreground = Brushes.White };
        help.Items.Add(CreateItem("About", window.HandleAbout));
        
        menu.Items.Add(file);
        menu.Items.Add(edit);
        menu.Items.Add(help);
        
        return menu;
    }
    
    public static Menu CreateVisualisedFormEditorMenu(MainWindow window)
    {
        var menu = new Menu { Background = new SolidColorBrush(Color.Parse("#1b5e20")) };
        
        // Grid with larger spacer before Preview
        menu.ItemsPanel = new FuncTemplate<Panel>(() =>
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 0: File
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 1: Edit
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 2: View
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 3: Tools
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 4: SPACER
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 5: Preview
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 6: Help
            return grid;
        });
        
        // FILE
        var file = new MenuItem { Header = "File", Foreground = Brushes.White };
        file.Items.Add(CreateItem("New Project", window.HandleNew, "Ctrl+N"));
        file.Items.Add(CreateItem("Open...", window.HandleOpen, "Ctrl+O"));
        file.Items.Add(CreateItem("Save", window.HandleSave, "Ctrl+S"));
        file.Items.Add(CreateItem("Save As...", window.HandleSaveAs, "Ctrl+Shift+S"));
        file.Items.Add(new Separator());
        file.Items.Add(CreateItem("Export...", window.HandleExport));
        file.Items.Add(new Separator());
        
        // Recent Projects submenu
        var recentMenu = new MenuItem { Header = "Recent Projects" };
        var recentProjects = ProjectManager.GetRecentProjects();
        if (recentProjects.Count > 0)
        {
            foreach (var proj in recentProjects)
            {
                var projName = Path.GetFileName(proj);
                var projItem = new MenuItem { Header = projName };
                projItem.Click += (s, e) => window.OpenProject(proj);
                recentMenu.Items.Add(projItem);
            }
        }
        else
        {
            recentMenu.Items.Add(new MenuItem { Header = "(No recent projects)", IsEnabled = false });
        }
        file.Items.Add(recentMenu);
        
        file.Items.Add(new Separator());
        file.Items.Add(CreateItem("Exit", window.HandleExit, "Alt+F4"));
        Grid.SetColumn(file, 0);
        
        // EDIT
        var edit = new MenuItem { Header = "Edit", Foreground = Brushes.White };
        edit.Items.Add(CreateItem("Undo", window.HandleUndo, "Ctrl+Z"));
        edit.Items.Add(CreateItem("Redo", window.HandleRedo, "Ctrl+Y"));
        edit.Items.Add(new Separator());
        edit.Items.Add(CreateItem("Cut", window.HandleCut, "Ctrl+X"));
        edit.Items.Add(CreateItem("Copy", window.HandleCopy, "Ctrl+C"));
        edit.Items.Add(CreateItem("Paste", window.HandlePaste, "Ctrl+V"));
        edit.Items.Add(CreateItem("Delete", window.HandleDelete, "Del"));
        edit.Items.Add(new Separator());
        edit.Items.Add(CreateItem("Select All", window.HandleSelectAll, "Ctrl+A"));
        edit.Items.Add(new Separator());
        edit.Items.Add(CreateItem("Edit VML Source...", window.HandleEditVML, "F12"));
        Grid.SetColumn(edit, 1);
        
        // VIEW
        var view = new MenuItem { Header = "View", Foreground = Brushes.White };
        view.Items.Add(CreateItem("Toolbox", window.HandleToolbox, "Ctrl+Alt+X"));
        view.Items.Add(CreateItem("Properties", window.HandleProperties, "F4"));
        view.Items.Add(CreateItem("Output", window.HandleOutput));
        view.Items.Add(new Separator());
        view.Items.Add(CreateItem("Zoom In", window.HandleZoomIn, "Ctrl++"));
        view.Items.Add(CreateItem("Zoom Out", window.HandleZoomOut, "Ctrl+-"));
        view.Items.Add(CreateItem("Zoom Reset", window.HandleZoomReset, "Ctrl+0"));
        view.Items.Add(new Separator());
        view.Items.Add(CreateItem("Grid Settings...", window.HandleGridSettings));
        Grid.SetColumn(view, 2);
        
        // TOOLS
        var tools = new MenuItem { Header = "Tools", Foreground = Brushes.White };
        tools.Items.Add(CreateItem("Menu Editor", window.HandleMenuEditor));
        tools.Items.Add(CreateItem("Options...", window.HandleOptions));
        Grid.SetColumn(tools, 3);
        
        // PREVIEW (Pinned Right with space)
        var preview = new MenuItem { Header = "Preview", Foreground = Brushes.White };
        preview.Click += window.HandleTogglePreview;
        Grid.SetColumn(preview, 5);
        
        // HELP (Pinned Right)
        var help = new MenuItem { Header = "Help", Foreground = Brushes.White };
        help.Items.Add(CreateItem("Documentation", window.HandleDocumentation, "F1"));
        help.Items.Add(CreateItem("VML Reference", window.HandleVMLReference));
        help.Items.Add(CreateItem("Sample Projects", window.HandleSamples));
        help.Items.Add(new Separator());
        help.Items.Add(CreateItem("Check for Updates", window.HandleUpdates));
        help.Items.Add(new Separator());
        help.Items.Add(CreateItem("About Visualised", window.HandleAbout));
        Grid.SetColumn(help, 6);
        
        menu.Items.Add(file);
        menu.Items.Add(edit);
        menu.Items.Add(view);
        menu.Items.Add(tools);
        menu.Items.Add(preview);
        menu.Items.Add(help);
        
        return menu;
    }
    
    public static Menu CreateSimpleTextEditorMenu(MainWindow window)
    {
        var menu = new Menu { Background = new SolidColorBrush(Color.Parse("#1b5e20")) };
        
        var file = new MenuItem { Header = "File", Foreground = Brushes.White };
        file.Items.Add(CreateItem("New", window.HandleNew, "Ctrl+N"));
        file.Items.Add(CreateItem("Open...", window.HandleOpen, "Ctrl+O"));
        file.Items.Add(CreateItem("Save", window.HandleSave, "Ctrl+S"));
        file.Items.Add(CreateItem("Save As...", window.HandleSaveAs));
        file.Items.Add(new Separator());
        file.Items.Add(CreateItem("Print", window.HandlePrint, "Ctrl+P"));
        file.Items.Add(new Separator());
        file.Items.Add(CreateItem("Exit", window.HandleExit, "Alt+F4"));
        
        var edit = new MenuItem { Header = "Edit", Foreground = Brushes.White };
        edit.Items.Add(CreateItem("Undo", window.HandleUndo, "Ctrl+Z"));
        edit.Items.Add(CreateItem("Redo", window.HandleRedo, "Ctrl+Y"));
        edit.Items.Add(new Separator());
        edit.Items.Add(CreateItem("Cut", window.HandleCut, "Ctrl+X"));
        edit.Items.Add(CreateItem("Copy", window.HandleCopy, "Ctrl+C"));
        edit.Items.Add(CreateItem("Paste", window.HandlePaste, "Ctrl+V"));
        edit.Items.Add(new Separator());
        edit.Items.Add(CreateItem("Find", window.HandleFind, "Ctrl+F"));
        edit.Items.Add(CreateItem("Replace", window.HandleReplace, "Ctrl+H"));
        
        var format = new MenuItem { Header = "Format", Foreground = Brushes.White };
        format.Items.Add(CreateItem("Font...", window.HandleFont));
        format.Items.Add(CreateItem("Word Wrap", window.HandleWordWrap));
        
        var help = new MenuItem { Header = "Help", Foreground = Brushes.White };
        help.Items.Add(CreateItem("About", window.HandleAbout));
        
        menu.Items.Add(file);
        menu.Items.Add(edit);
        menu.Items.Add(format);
        menu.Items.Add(help);
        
        return menu;
    }
    
    public static ContextMenu CreateBasicContextMenu(MainWindow window)
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateItem("Cut", window.HandleCut));
        menu.Items.Add(CreateItem("Copy", window.HandleCopy));
        menu.Items.Add(CreateItem("Paste", window.HandlePaste));
        return menu;
    }
    
    public static ContextMenu CreateVisualisedEditorContextMenu(MainWindow window)
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateItem("Cut", window.HandleCut, "Ctrl+X"));
        menu.Items.Add(CreateItem("Copy", window.HandleCopy, "Ctrl+C"));
        menu.Items.Add(CreateItem("Paste", window.HandlePaste, "Ctrl+V"));
        menu.Items.Add(CreateItem("Delete", window.HandleDelete, "Del"));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateItem("Edit Script...", window.HandleEditScript, "F2"));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateItem("Bring to Front", window.HandleBringToFront, "Ctrl+]"));
        menu.Items.Add(CreateItem("Send to Back", window.HandleSendToBack, "Ctrl+["));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateItem("Align", window.HandleAlign));
        menu.Items.Add(CreateItem("Size", window.HandleSize));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateItem("Properties", window.HandleProperties, "F4"));
        return menu;
    }
    
    public static ContextMenu CreateTextEditorContextMenu(MainWindow window)
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateItem("Undo", window.HandleUndo));
        menu.Items.Add(CreateItem("Redo", window.HandleRedo));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateItem("Cut", window.HandleCut));
        menu.Items.Add(CreateItem("Copy", window.HandleCopy));
        menu.Items.Add(CreateItem("Paste", window.HandlePaste));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateItem("Select All", window.HandleSelectAll));
        return menu;
    }
    
    private static MenuItem CreateItem(string header, EventHandler<RoutedEventArgs> handler, string? shortcut = null)
    {
        var item = new MenuItem { Header = shortcut != null ? $"{header}\t{shortcut}" : header };
        item.Click += handler;
        return item;
    }
}

