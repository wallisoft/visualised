using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            // Load and parse VML
            var controls = VmlLoader.Load(vmlPath);
            
            // First pass: Register @Script objects
            foreach (var ctrl in controls.Where(c => c.Type == "Script"))
            {
                var name = ctrl.Name ?? "unnamed_script";
                var scriptContent = ctrl.Properties.GetValueOrDefault("Content", "");
                var interpreter = ctrl.Properties.GetValueOrDefault("Interpreter", "bash");
                
                ScriptRegistry.Register(name, scriptContent, interpreter);
            }
            
            // Build window from VML properties
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
                Console.WriteLine($"[VML LOADER] Built UI from {Path.GetFileName(vmlPath)} ({ScriptRegistry.Count} scripts registered)");
                
                // Auto-wire event handlers after window opens
                window.Opened += (s, e) => AutoWireScriptHandlers(window, controls);
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
            if (trimmed.StartsWith("@")) continue; // Skip object definitions
            
            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
                props[parts[0].Trim()] = parts[1].Trim();
        }
        return props;
    }
    
    private static void AutoWireScriptHandlers(Window window, List<VmlControl> controls)
    {
        Console.WriteLine("[VML LOADER] Auto-wiring script handlers...");
        
        foreach (var ctrl in controls.Where(c => c.Type != "Script"))
        {
            var controlName = ctrl.Name;
            if (string.IsNullOrEmpty(controlName)) continue;
            
            // Find the actual control in the visual tree
            var actualControl = FindControlByName(window, controlName);
            if (actualControl == null)
            {
                Console.WriteLine($"[VML LOADER] Control not found: {controlName}");
                continue;
            }
            
            // Check for event handlers
            foreach (var prop in ctrl.Properties)
            {
                if (prop.Key.StartsWith("On"))
                {
                    var eventName = prop.Key.Substring(2); // Remove "On" prefix
                    var scriptName = prop.Value;
                    
                    Console.WriteLine($"[VML LOADER] Wiring {controlName}.{eventName} → {scriptName}");
                    WireScriptHandler(actualControl, eventName, scriptName);
                }
            }
        }
    }
    
    private static Control? FindControlByName(Control parent, string name)
    {
        if (parent.Name == name)
            return parent;
        
        foreach (var child in parent.GetVisualChildren().OfType<Control>())
        {
            var result = FindControlByName(child, name);
            if (result != null) return result;
        }
        
        return null;
    }
    
    public static void WireScriptHandler(Control control, string eventName, string scriptName)
    {
        var script = ScriptRegistry.Get(scriptName);
        if (script == null)
        {
            Console.WriteLine($"[VML LOADER] Script not found: {scriptName}");
            return;
        }
        
        // Wire the event based on type
        if (control is Button button && eventName == "Click")
        {
            button.Click += (s, e) => ExecuteScript(script, control);
        }
        else if (control is ListBox listBox && eventName == "SelectionChanged")
        {
            listBox.SelectionChanged += (s, e) => ExecuteScript(script, control);
        }
        // Add more event types as needed
        
        Console.WriteLine($"[VML LOADER] Wired: {control.Name}.{eventName} → {scriptName}");
    }
    
    private static void ExecuteScript(VmlScript script, Control control)
    {
        var args = new Dictionary<string, string>
        {
            { "CONTROLNAME", control.Name ?? "unnamed" },
            { "CONTROLTYPE", control.GetType().Name }
        };
        
        Console.WriteLine($"[SCRIPT] Executing: {script.Name}");
        ScriptHandler.Execute(script.Content, script.Interpreter, args);
    }
}
