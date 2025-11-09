using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
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
        
        // Get all settable properties via reflection
        var props = control.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.CanRead)
            .OrderBy(p => p.Name);
        
        // Control type selector as first property
        AddControlSelector();
        
        foreach (var prop in props)
        {
            // Skip complex types we don't want to edit
            if (ShouldSkip(prop)) continue;
            
            AddPropertyEditor(control, prop);
        }
    }
    

    private void AddPropertyEditor(Control control, PropertyInfo prop)
    {
        var row = CreateRow();
        row.Children.Add(CreateLabel(prop.Name + ":"));
        
        Control editor = prop.PropertyType.Name switch
        {
            "String" => CreateTextBox(control, prop),
            "Double" or "Int32" => CreateNumberBox(control, prop),
            "Boolean" => CreateCheckBox(control, prop),
            "Brush" or "IBrush" => CreateColorPicker(control, prop),
            _ when prop.PropertyType.IsEnum => CreateEnumCombo(control, prop),
            _ => new TextBlock { Text = "(complex)" }
        };
        
        row.Children.Add(editor);
        panel.Children.Add(row);
    }
    
    private StackPanel CreateRow() => new() 
    { 
        Orientation = Orientation.Horizontal, 
        Spacing = 2, 
        Margin = new Thickness(0, 0, 0, 3) 
    };
    
    private TextBlock CreateLabel(string text) => new() 
    { 
        Text = text,
        Width = 60,
        TextAlignment = TextAlignment.Right,
        Margin = new Thickness(0, 0, 5, 0)
    };
    
    private TextBox CreateTextBox(Control control, PropertyInfo prop)
    {
        var tb = new TextBox 
        { 
            Width = 120, 
            Height = 20, MinHeight = 20, MaxHeight = 20,
            Padding = new Thickness(4, 2, 4, 2),
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(1)
        };
        tb.Text = prop.GetValue(control)?.ToString() ?? "";
        tb.LostFocus += (s, e) => prop.SetValue(control, tb.Text);
        return tb;
    }
    
    private TextBox CreateNumberBox(Control control, PropertyInfo prop)
    {
        var tb = new TextBox { Width = 120, Height = 20, MinHeight = 20, MaxHeight = 20, Padding = new Thickness(4, 2, 4, 2), BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")), BorderThickness = new Thickness(1) };
        tb.Text = prop.GetValue(control)?.ToString() ?? "0";
        tb.LostFocus += (s, e) => {
            if (double.TryParse(tb.Text, out var val))
                prop.SetValue(control, Convert.ChangeType(val, prop.PropertyType));
        };
        return tb;
    }
    
    private CheckBox CreateCheckBox(Control control, PropertyInfo prop)
    {
        var cb = new CheckBox { IsChecked = (bool?)prop.GetValue(control) };
        cb.Click += (s, e) => prop.SetValue(control, cb.IsChecked);
        return cb;
    }
    
    private Button CreateColorPicker(Control control, PropertyInfo prop)
    {
        var btn = new Button { Content = "📎", Width = 30, Height = 20 };
        btn.Click += async (s, e) => {
            // TODO: Color picker dialog
        };
        return btn;
    }
    
    private ComboBox CreateEnumCombo(Control control, PropertyInfo prop)
    {
        var combo = new ComboBox { Width = 120, Height = 20, MinHeight = 20, MaxHeight = 20, Padding = new Thickness(4, 2, 4, 2), BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")), BorderThickness = new Thickness(1) };
        foreach (var val in Enum.GetValues(prop.PropertyType))
            combo.Items.Add(val);
        combo.SelectedItem = prop.GetValue(control);
        combo.SelectionChanged += (s, e) => {
            if (combo.SelectedItem != null)
                prop.SetValue(control, combo.SelectedItem);
        };
        return combo;
    }
    
    private void AddControlSelector()
    {
        var row = CreateRow();
        row.Children.Add(CreateLabel("Add:"));
        
        var combo = new ComboBox { Width = 120, Height = 20, MinHeight = 20, MaxHeight = 20, Padding = new Thickness(4,2,4,2), BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")), BorderThickness = new Thickness(1) };
        var types = new[] { "Button", "TextBox", "Label", "CheckBox", "ComboBox", "ListBox", "Grid", "StackPanel", "Border" };
        foreach (var t in types) combo.Items.Add(t);
        combo.SelectedIndex = 0;
        
        row.Children.Add(combo);
        panel.Children.Add(row);
    }
    
    private bool ShouldSkip(PropertyInfo prop)
    {
        var skip = new[] { "Parent", "DataContext", "Resources", "Styles", "Classes", "CommandBindings" };
        return skip.Contains(prop.Name) || 
               prop.PropertyType.IsGenericType ||
               prop.PropertyType.IsArray;
    }

    private Button CreateTinyButton(string content, int width)
    {
        return new Button
        {
            Content = content,
            Width = width,
            Height = 18,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(2, 1, 2, 3),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Left
        };
    }
}
