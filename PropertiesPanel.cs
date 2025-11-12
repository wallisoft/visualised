using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;


namespace VB;

public class PropertiesPanel
{
    private StackPanel panel;
    private Control? selectedControl;
    
    public PropertiesPanel(StackPanel targetPanel)
    {
        panel = targetPanel;
    }
    
	public void ShowPropertiesFor(Control control)
	{
	    selectedControl = control;
	    panel.Children.Clear();
	    
	    var controlType = control.GetType().Name;
	    
	    // Get property display order from database
	    var dbPath = Path.Combine(Environment.CurrentDirectory, "visualised.db");
	    using var conn = new SqliteConnection($"Data Source={dbPath}");
	    conn.Open();
	    
	    var displayRules = new Dictionary<string, (int order, bool hidden)>();
	    
	    // First query - property display rules
	    using (var cmd = conn.CreateCommand())
	    {
		cmd.CommandText = @"
		    SELECT property_name, display_order, is_hidden 
		    FROM property_display 
		    WHERE control_type IN ('*', @controlType) 
		    ORDER BY 
			CASE WHEN control_type = @controlType THEN 0 ELSE 1 END,
			display_order";
		cmd.Parameters.AddWithValue("@controlType", controlType);
		
		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
		    var propName = reader.GetString(0);
		    var order = reader.GetInt32(1);
		    var hidden = reader.GetInt32(2) == 1;
		    displayRules[propName] = (order, hidden);
		}
	    }
	    
	    // Second query - property groups
	    var groups = new List<(string name, string display, string[] components, string picker)>();
	    using (var cmd = conn.CreateCommand())
	    {
		cmd.CommandText = "SELECT group_name, display_name, component_properties, picker_type FROM property_groups ORDER BY display_order";
		using var groupReader = cmd.ExecuteReader();
		while (groupReader.Read())
		{
		    groups.Add((
			groupReader.GetString(0),
			groupReader.GetString(1),
			groupReader.GetString(2).Split(','),
			groupReader.GetString(3)
		    ));
		}
	    }
	    
	    // Add property groups first
	    foreach (var group in groups.Where(g => displayRules.ContainsKey(g.name) && !displayRules[g.name].hidden))
	    {
		if (group.picker == "TinyFontPicker")
		    AddFontRow(control, group.display);
	    }
	    
	    // Get all properties
	    var props = control.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
		.Where(p => p.CanWrite && p.CanRead && !ShouldSkip(p))
		.Where(p => !displayRules.ContainsKey(p.Name) || !displayRules[p.Name].hidden)
		.OrderBy(p => displayRules.ContainsKey(p.Name) ? displayRules[p.Name].order : 1000)
		.ThenBy(p => p.Name);
	    
	    foreach (var prop in props)
	    {
		AddPropertyRow(control, prop);
	    }
	}

private void AddFontRow(Control control, string displayName)
{
    var row = new StackPanel 
    { 
        Orientation = Orientation.Horizontal, 
        Spacing = 5,
        Margin = new Thickness(0, 0, 0, 2)
    };
    
    var label = new TextBlock 
    { 
        Text = displayName,
        Width = 60,
        FontSize = 11,
        TextAlignment = TextAlignment.Right,
        VerticalAlignment = VerticalAlignment.Center
    };
    row.Children.Add(label);
    
    var fontPicker = new TinyFontPicker();
    
    // Get current font properties
    var familyProp = control.GetType().GetProperty("FontFamily");
    var sizeProp = control.GetType().GetProperty("FontSize");
    var weightProp = control.GetType().GetProperty("FontWeight");
    var styleProp = control.GetType().GetProperty("FontStyle");
    
    if (familyProp != null && familyProp.GetValue(control) != null)
        fontPicker.Family = (FontFamily)familyProp.GetValue(control)!;
    if (sizeProp != null && sizeProp.GetValue(control) != null)
        fontPicker.Size = (double)sizeProp.GetValue(control)!;
    if (weightProp != null && weightProp.GetValue(control) != null)
        fontPicker.Weight = (FontWeight)weightProp.GetValue(control)!;
    if (styleProp != null && styleProp.GetValue(control) != null)
        fontPicker.Style = (FontStyle)styleProp.GetValue(control)!;
    
    fontPicker.FontChanged += (s, font) =>
    {
        familyProp?.SetValue(control, font.family);
        sizeProp?.SetValue(control, font.size);
        weightProp?.SetValue(control, font.weight);
        styleProp?.SetValue(control, font.style);
    };
    
    row.Children.Add(fontPicker);
    panel.Children.Add(row);
}

    private void AddPropertyRow(Control control, PropertyInfo prop)
    {
        var row = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            Spacing = 5,
            Margin = new Thickness(0, 0, 0, 2)
        };

	// Debug - check what we're getting for color properties
	if (prop.Name == "Background" || prop.Name == "Foreground" || prop.Name == "BorderBrush")
	{
	    Console.WriteLine($"[DEBUG] {prop.Name}: Type={prop.PropertyType.Name}, FullName={prop.PropertyType.FullName}");
	}
        
        // Left label - 11px normal
        var label = new TextBlock 
        { 
            Text = prop.Name + ":",
            Width = 80,
            FontSize = 11,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Children.Add(label);
        
        // Create appropriate editor based on type
        if (prop.PropertyType == typeof(string))
            row.Children.Add(CreateTinyTextBox(control, prop));
        else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(int))
            row.Children.Add(CreateTinyTextBox(control, prop));
        else if (prop.PropertyType == typeof(bool))
            row.Children.Add(new CheckBox { IsChecked = (bool?)prop.GetValue(control),Height = 20 });
	else if (prop.PropertyType.Name.Contains("Brush") || prop.PropertyType.Name == "IBrush")
            row.Children.Add(CreateTinyColorPicker(control, prop));
        else if (prop.PropertyType.IsEnum)
            row.Children.Add(CreateTinyCombo(control, prop));
        else
            row.Children.Add(new TextBlock { Text = "(complex)", FontSize = 11 });
        

        panel.Children.Add(row);
    }
    
    private Control CreateTinyTextBox(Control control, PropertyInfo prop)
    {
        var tiny = new TinyTextBox
        {
            Text = prop.GetValue(control)?.ToString() ?? ""
        };
        
        tiny.TextChanged += (s, text) =>
        {
            // Skip Name - can't change after styled
            if (prop.Name == "Name") return;
            
            object value = text;
            if (prop.PropertyType == typeof(double))
            {
                if (double.TryParse(text, out var d))
                    value = d;
            }
            else if (prop.PropertyType == typeof(int))
            {
                if (int.TryParse(text, out var i))
                    value = i;
            }
            prop.SetValue(control, value);
        };
        
        return tiny;
    }
    private Control CreateTinyCombo(Control control, PropertyInfo prop)
    {
        var tiny = new TinyCombo();
        
        // Add enum values
        foreach (var val in Enum.GetValues(prop.PropertyType))
            tiny.Items.Add(val);
        
        // Set current value
        var currentVal = prop.GetValue(control);
        tiny.Text = currentVal?.ToString() ?? "";
        
        tiny.SelectionChanged += (s, selectedItem) =>
        {
            prop.SetValue(control, selectedItem);
        };
        
        return tiny;
    }

    private Control CreateTinyColorPicker(Control control, PropertyInfo prop)
    {
	    var picker = new TinyColorPicker();
	    var brush = prop.GetValue(control) as ISolidColorBrush;
	    if (brush != null) picker.Color = brush.Color;
	    
	    picker.ColorChanged += (s, color) => prop.SetValue(control, new SolidColorBrush(color));
	    return picker;
    }


    private bool ShouldSkip(PropertyInfo prop)
    {
        // Only skip truly dangerous/system properties
        var skip = new[] { "Parent", "DataContext" };
        return skip.Contains(prop.Name);
    }
}
