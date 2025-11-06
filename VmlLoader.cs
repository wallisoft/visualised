using System;
using System.Collections.Generic;
using System.IO;

namespace VB;

public class VmlLoader
{
    public static List<VmlControl> Load(string filePath)
    {
        var controls = new List<VmlControl>();
        VmlControl? current = null;
        
        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();
            
            // Skip blank lines and comments
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                continue;
                
            if (trimmed.StartsWith("@"))
            {
                if (current != null)
                    controls.Add(current);
                    
                var parts = trimmed.Substring(1).Split(' ', 2);
                current = new VmlControl
                {
                    Type = parts[0],
                    Name = parts.Length > 1 ? parts[1] : null,
                    Properties = new Dictionary<string, string>()
                };
            }
            else if (current != null && line.Length > 0 && char.IsWhiteSpace(line[0]))
            {
                // This is a property line (starts with any whitespace)
                var parts = trimmed.Split('=', 2);
                if (parts.Length == 2)
                {
                    current.Properties[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }
        
        if (current != null)
            controls.Add(current);
            
        return controls;
    }
}

public class VmlControl
{
    public string Type { get; set; } = "";
    public string? Name { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

