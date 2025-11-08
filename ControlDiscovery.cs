using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;

namespace VB;

public class ControlDiscovery
{
    public static List<Type> GetAllControlTypes()
    {
        var assembly = typeof(Button).Assembly; // Avalonia.Controls assembly
        
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(Control).IsAssignableFrom(t))
            .Where(t => t.IsPublic)
            .Where(t => HasParameterlessConstructor(t))
            .OrderBy(t => t.Name)
            .ToList();
    }
    
    private static bool HasParameterlessConstructor(Type type)
    {
        return type.GetConstructor(Type.EmptyTypes) != null;
    }
    
    public static List<string> GetCommonControls()
    {
        return new List<string>
        {
            "Button", "TextBlock", "TextBox", "CheckBox", "RadioButton",
            "ComboBox", "ListBox", "StackPanel", "Grid", "Border",
            "Canvas", "DockPanel", "WrapPanel", "ScrollViewer",
            "Expander", "TabControl", "Label", "Slider", "ProgressBar"
        };
    }
}

