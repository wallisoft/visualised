using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VB;

public class VmlLoader
{
    public static List<VmlControl> Load(string filePath)
    {
        var controls = new List<VmlControl>();
        VmlControl? current = null;
        string? heredocMarker = null;
        var heredocContent = new StringBuilder();
        string? heredocProperty = null;
        
        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();
            
            // Handle heredoc content
            if (heredocMarker != null)
            {
                if (trimmed == heredocMarker)
                {
                    // End of heredoc
                    if (current != null && heredocProperty != null)
                    {
                        current.Properties[heredocProperty] = heredocContent.ToString();
                    }
                    heredocMarker = null;
                    heredocContent.Clear();
                    heredocProperty = null;
                }
                else
                {
                    heredocContent.AppendLine(line);
                }
                continue;
            }
            
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
                    var propName = parts[0].Trim();
                    var propValue = parts[1].Trim();
                    
                    // Check for heredoc syntax (<<EOF)
                    if (propValue.StartsWith("<<"))
                    {
                        heredocMarker = propValue.Substring(2).Trim();
                        heredocProperty = propName;
                        heredocContent.Clear();
                    }
                    else
                    {
                        current.Properties[propName] = propValue;
                    }
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

