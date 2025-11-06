using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.IO;

namespace VB;

public static class VmlWindowLoader
{
    public static Window? LoadWindow(string vmlPath, Dictionary<string, object>? context = null)
    {
        if (!File.Exists(vmlPath))
        {
            Console.WriteLine($"[VML LOADER] File not found: {vmlPath}");
            return null;
        }
        
        try
        {
            var vml = File.ReadAllText(vmlPath);
            var props = ParseVML(vml);
            
            var window = new Window();
            
            if (props.TryGetValue("Title", out var title)) 
                window.Title = title;
            if (props.TryGetValue("Width", out var w) && int.TryParse(w, out var width)) 
                window.Width = width;
            if (props.TryGetValue("Height", out var h) && int.TryParse(h, out var height)) 
                window.Height = height;
            
            if (context != null)
                window.DataContext = context;
            
            // Build UI from VML
            var content = VmlUiBuilder.BuildFromVml(props);
            if (content != null)
            {
                window.Content = content;
                Console.WriteLine($"[VML LOADER] Built UI from {Path.GetFileName(vmlPath)}");
            }
            else
            {
                window.Content = new TextBlock 
                { 
                    Text = $"Failed to build UI from VML",
                    Margin = new Avalonia.Thickness(20)
                };
            }
            
            return window;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VML LOADER] Error: {ex.Message}");
            return null;
        }
    }
    
    private static Dictionary<string, string> ParseVML(string vml)
    {
        var props = new Dictionary<string, string>();
        foreach (var line in vml.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
            
            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
                props[parts[0].Trim()] = parts[1].Trim();
        }
        return props;
    }
}

