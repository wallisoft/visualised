using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace VB;

/// <summary>
/// Simple VML Parser - Handles @Control definitions and Property=Value format
/// Imports VML to ui_tree/ui_properties and exports back to VML
/// No complex validation - just clean, direct parsing
/// </summary>
public class VmlDatabaseParser
{
    private readonly string _dbPath;
    
    public VmlDatabaseParser(string dbPath)
    {
        _dbPath = dbPath;
    }
    
    // ========================================
    // VML → DATABASE
    // ========================================
    
    public void ImportVml(string vmlPath)
    {
        Console.WriteLine($"[VML PARSER] Reading {vmlPath}");
        
        var lines = File.ReadAllLines(vmlPath);
        var controls = ParseVmlControls(lines);
        
        Console.WriteLine($"[VML PARSER] Parsed {controls.Count} controls");
        
        WriteToDatabase(controls);
        
        Console.WriteLine($"[VML PARSER] ✓ Imported to database");
    }
    
    private List<VmlControl> ParseVmlControls(string[] lines)
    {
        var controls = new List<VmlControl>();
        VmlControl? current = null;
        bool inHeredoc = false;
        string heredocContent = "";
        string heredocProperty = "";
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Handle heredoc content (<<EOF...EOF)
            if (inHeredoc)
            {
                if (trimmed == "EOF")
                {
                    if (current != null)
                        current.Properties[heredocProperty] = heredocContent.Trim();
                    inHeredoc = false;
                    heredocContent = "";
                    continue;
                }
                heredocContent += line + "\n";
                continue;
            }
            
            // Skip comments and blank lines
            if (trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed))
                continue;
            
            // Control definition: @Grid MainGrid
            if (trimmed.StartsWith("@"))
            {
                var parts = trimmed.Substring(1).Split(new[] { ' ' }, 2);
                current = new VmlControl
                {
                    Type = parts[0],
                    Name = parts.Length > 1 ? parts[1] : parts[0] + "_" + controls.Count
                };
                controls.Add(current);
                continue;
            }
            
            // Property: Width=800 or Content=<<EOF
            if (current != null && trimmed.Contains("="))
            {
                var eqIndex = trimmed.IndexOf('=');
                var key = trimmed.Substring(0, eqIndex).Trim();
                var value = trimmed.Substring(eqIndex + 1).Trim();
                
                // Check for heredoc
                if (value == "<<EOF")
                {
                    inHeredoc = true;
                    heredocProperty = key;
                    continue;
                }
                
                current.Properties[key] = value;
            }
        }
        
        return controls;
    }
    
    private void WriteToDatabase(List<VmlControl> controls)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        
        // Clear existing data
        using var clearCmd = conn.CreateCommand();
        clearCmd.CommandText = "DELETE FROM ui_properties; DELETE FROM ui_tree;";
        clearCmd.ExecuteNonQuery();
        
        // Build hierarchy map
        var controlIds = new Dictionary<string, int>();
        var nextId = 1;
        
        // First pass: Insert all controls
        foreach (var control in controls)
        {
            var parentName = control.Properties.ContainsKey("Parent") 
                ? control.Properties["Parent"] 
                : null;
            
            var parentId = parentName != null && controlIds.ContainsKey(parentName)
                ? controlIds[parentName]
                : (int?)null;
            
            var isRoot = parentId == null ? 1 : 0;
            
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ui_tree (id, parent_id, control_type, name, display_order, is_root)
                VALUES (@id, @parent_id, @type, @name, @order, @root)";
            
            cmd.Parameters.AddWithValue("@id", nextId);
            cmd.Parameters.AddWithValue("@parent_id", parentId.HasValue ? (object)parentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@type", control.Type);
            cmd.Parameters.AddWithValue("@name", control.Name);
            cmd.Parameters.AddWithValue("@order", controls.IndexOf(control));
            cmd.Parameters.AddWithValue("@root", isRoot);
            cmd.ExecuteNonQuery();
            
            controlIds[control.Name] = nextId;
            
            // Insert properties
            foreach (var prop in control.Properties)
            {
                if (prop.Key == "Parent") continue; // Skip Parent - already used for hierarchy
                
                using var propCmd = conn.CreateCommand();
                propCmd.CommandText = @"
                    INSERT INTO ui_properties (ui_tree_id, property_name, property_value)
                    VALUES (@id, @name, @value)";
                
                propCmd.Parameters.AddWithValue("@id", nextId);
                propCmd.Parameters.AddWithValue("@name", prop.Key);
                propCmd.Parameters.AddWithValue("@value", prop.Value);
                propCmd.ExecuteNonQuery();
            }
            
            nextId++;
        }
    }
    
    // ========================================
    // DATABASE → VML
    // ========================================
    
    public void ExportVml(string vmlPath)
    {
        Console.WriteLine($"[VML PARSER] Exporting to {vmlPath}");
        
        var controls = ReadFromDatabase();
        
        var lines = new List<string>
        {
            "# Exported VML",
            $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            ""
        };

        foreach (var control in controls)
        {
            lines.Add($"@{control.Type} {control.Name}");
            
            foreach (var prop in control.Properties.OrderBy(p => p.Key))
            {
                var value = prop.Value;
                
                // Use heredoc for multiline content
                if (value.Contains("\n"))
                {
                    lines.Add($"{prop.Key}=<<EOF");
                    lines.Add(value);
                    lines.Add("EOF");
                }
                else
                {
                    lines.Add($"{prop.Key}={value}");
                }
            }
            
            lines.Add(""); // Blank line between controls
        }
        
        File.WriteAllLines(vmlPath, lines);
        
        Console.WriteLine($"[VML PARSER] ✓ Exported {controls.Count} controls");
    }
    
    private List<VmlControl> ReadFromDatabase()
    {
        var controls = new List<VmlControl>();
        
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        
        // Get all controls
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, parent_id, control_type, name FROM ui_tree ORDER BY display_order";
        
        using var reader = cmd.ExecuteReader();
        var controlMap = new Dictionary<int, VmlControl>();
        
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var parentId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
            var type = reader.GetString(2);
            var name = reader.GetString(3);
            
            var control = new VmlControl { Type = type, Name = name };
            controlMap[id] = control;
            controls.Add(control);
            
            // Add Parent property if has parent
            if (parentId.HasValue && controlMap.ContainsKey(parentId.Value))
            {
                control.Properties["Parent"] = controlMap[parentId.Value].Name;
            }
        }
        reader.Close();
        
        // Load properties for each control
        foreach (var kvp in controlMap)
        {
            using var propCmd = conn.CreateCommand();
            propCmd.CommandText = "SELECT property_name, property_value FROM ui_properties WHERE ui_tree_id = @id";
            propCmd.Parameters.AddWithValue("@id", kvp.Key);
            
            using var propReader = propCmd.ExecuteReader();
            while (propReader.Read())
            {
                var propName = propReader.GetString(0);
                var propValue = propReader.GetString(1);
                kvp.Value.Properties[propName] = propValue;
            }
        }
        
        return controls;
    }
}

/// <summary>
/// Simple VML control representation
/// </summary>
public class VmlControliDef
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public Dictionary<string, string> Properties { get; set; } = new();
}
