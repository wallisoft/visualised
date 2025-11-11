using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Reflection;
using System.Linq;

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
        
        var props = control.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.CanRead && !ShouldSkip(p))
            .OrderBy(p => 
        {
            // TODO: Query from property_order table
            // For now, prioritize common properties
            // Order by usage: Usual Suspects, Size/Pos, Font, Colors, Alignment, Border, Rest
            var priority = new[] { 
                // Usual suspects
                "Name", "Content", "Text", 
                // Size & Position
                "Width", "Height", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight",
                "Margin", "Padding",
                // Font properties
                "FontSize", "FontWeight", "FontStyle", "FontFamily", "FontStretch",
                // Colors
                "Background", "Foreground", "BorderBrush", 
                // Alignment
                "HorizontalAlignment", "VerticalAlignment", 
                "HorizontalContentAlignment", "VerticalContentAlignment",
                // Border
                "BorderThickness", "CornerRadius",
                // Common UI
                "IsVisible", "IsEnabled", "Opacity",
                "Cursor"
            };
            var index = Array.IndexOf(priority, p.Name);
            return index == -1 ? 100 + p.Name.GetHashCode() : index;
        });
        
        foreach (var prop in props)
        {
            AddPropertyRow(control, prop);
        }
    }
    
    private void AddPropertyRow(Control control, PropertyInfo prop)
    {
        var row = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            Spacing = 5,
            Margin = new Thickness(0, 0, 0, 2)
        };
        
        // Left label - 11px normal
        var label = new TextBlock 
        { 
            Text = prop.Name + ":",
            Width = 60,
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
            row.Children.Add(new CheckBox { IsChecked = (bool?)prop.GetValue(control) });
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
