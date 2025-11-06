using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using System.IO;

namespace VB;

public class PropertyCatalog
{
    public static void GenerateCatalog()
    {
        var output = new List<string>();
        output.Add("# Avalonia Controls Property Catalog");
        output.Add($"Generated: {DateTime.Now}");
        output.Add("");
        
        var assembly = typeof(Button).Assembly;
        var controlTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(Control).IsAssignableFrom(t))
            .Where(t => t.IsPublic)
            .OrderBy(t => t.Name)
            .ToList();
        
        Console.WriteLine($"Found {controlTypes.Count} control types");
        
        foreach (var controlType in controlTypes)
        {
            output.Add($"\n## {controlType.Name}");
            output.Add($"Namespace: {controlType.Namespace}");
            output.Add("");
            
            var props = controlType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .OrderBy(p => p.Name)
                .ToList();
            
            foreach (var prop in props)
            {
                output.Add($"  {prop.Name}: {prop.PropertyType.Name}");
            }
            
            output.Add("");
        }
        
        File.WriteAllLines("avalonia-properties.txt", output);
        Console.WriteLine("Catalog written to avalonia-properties.txt");
    }
}

