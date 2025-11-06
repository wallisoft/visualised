using Avalonia.Controls;

namespace VB;

public class TestMenuWindow : Window
{
    public TestMenuWindow()
    {
        Width = 600;
        Height = 400;
        Title = "Property Test";
        
        var dock = new DockPanel();
        var menu = new Menu();
        DockPanel.SetDock(menu, Dock.Top);
        
        var fileMenu = new MenuItem { Header = "File" };
        var newItem = new MenuItem { Header = "New" };
        
        fileMenu.Items.Add(newItem);
        menu.Items.Add(fileMenu);
        dock.Children.Add(menu);
        
        Content = dock;
        
        // Dump properties
        PropertyDumper.DumpControl(this, "Window");
        PropertyDumper.DumpControl(dock, "DockPanel");
        PropertyDumper.DumpControl(menu, "Menu");
        PropertyDumper.DumpControl(fileMenu, "MenuItem-File");
    }
}

