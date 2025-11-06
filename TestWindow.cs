using Avalonia.Controls;
using Avalonia.Media;

namespace VB;

public class TestWindow : Window
{
    public TestWindow()
    {
        Width = 600;
        Height = 400;
        Title = "Pure C# Test";
        
        var dock = new DockPanel 
        { 
            LastChildFill = true,
            Background = Brushes.Black
        };
        
        var left = new Border 
        { 
            Width = 100,
            MinHeight = 100,
            Background = Brushes.Red
        };
        DockPanel.SetDock(left, Dock.Left);
        
        var right = new Border 
        { 
            Width = 100,
            MinHeight = 100,
            Background = Brushes.Blue
        };
        DockPanel.SetDock(right, Dock.Right);
        
        var center = new Border 
        { 
            Background = Brushes.Green
        };
        
        dock.Children.Add(left);
        dock.Children.Add(right);
        dock.Children.Add(center);
        
        Content = dock;
        
        System.Console.WriteLine($"DockPanel children: {dock.Children.Count}");
        System.Console.WriteLine($"Left dock: {DockPanel.GetDock(left)}");
        System.Console.WriteLine($"Right dock: {DockPanel.GetDock(right)}");
    }
}

