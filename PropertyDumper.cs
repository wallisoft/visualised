using System;
using System.Reflection;
using Avalonia.Controls;

namespace VB;

public static class PropertyDumper
{
    public static void DumpControl(Control control, string name)
    {
        Console.WriteLine($"\n=== {name} ({control.GetType().Name}) ===");
        
        var props = control.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var prop in props)
        {
            if (!prop.CanRead) continue;
            
            try
            {
                var value = prop.GetValue(control);
                if (value != null)
                {
                    Console.WriteLine($"  {prop.Name} = {value}");
                }
            }
            catch
            {
                // Skip properties that throw
            }
        }
    }
}

