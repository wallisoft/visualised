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
            row.Children.Add(new TextBlock { Text = "🎨", FontSize = 14 }); // TODO: Color picker
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
        Console.WriteLine($"[COMBO] Loading TinyCombo for {prop.Name} ({prop.PropertyType.Name})");
        // Load TinyCombo from database
        var vmlControls = VmlLoader.LoadFromDatabase("TinyCombo");
        Console.WriteLine($"[COMBO] Loaded {vmlControls.Count} controls from database");
        if (vmlControls.Count > 0)
        {
            // Flatten tree into list for ControlBuilder
            var flatControls = VmlLoader.FlattenControls(vmlControls);
            Console.WriteLine($"[COMBO] Flattened to {flatControls.Count} controls");
            var builder = new ControlBuilder(flatControls);
            var controls = builder.BuildControls();
            if (controls.Count > 0 && controls[0] is Panel container)
            {
                Console.WriteLine($"[COMBO] Container has {container.Children.Count} children");
                var valueBox = container.Children.OfType<Label>().FirstOrDefault();
                var dropBtn = container.Children.OfType<Button>().FirstOrDefault();
                Console.WriteLine($"[COMBO] valueBox={valueBox?.Name}, dropBtn={dropBtn?.Name}");
                
                if (valueBox != null && dropBtn != null)
                {
                    valueBox.Content = prop.GetValue(control)?.ToString() ?? "";
                    
                    dropBtn.Click += (s, e) =>
                    {
                        var parent = container.Parent as Panel;
                        if (parent == null) return;
                        
                        var combo = new ComboBox { Width = 140, MinHeight = 19 };
                        foreach (var val in Enum.GetValues(prop.PropertyType))
                            combo.Items.Add(val);
                        combo.SelectedItem = prop.GetValue(control);
                        
                        combo.SelectionChanged += (s2, e2) =>
                        {
                            if (combo.SelectedItem != null)
                            {
                                valueBox.Content = combo.SelectedItem.ToString();
                                prop.SetValue(control, combo.SelectedItem);
                            }
                        };
                        
                        combo.DropDownClosed += (s2, e2) =>
                        {
                            var idx = parent.Children.IndexOf(combo);
                            parent.Children.RemoveAt(idx);
                            parent.Children.Insert(idx, container);
                        };
                        
                        var index = parent.Children.IndexOf(container);
                        parent.Children.RemoveAt(index);
                        parent.Children.Insert(index, combo);
                        combo.Focus();
                        combo.IsDropDownOpen = true;
                    };
                }
                
                return container;
            }
        }
        
        return new TextBlock { Text = "(enum)" };
    }
    
    private bool ShouldSkip(PropertyInfo prop)
    {
        // Only skip truly dangerous/system properties
        var skip = new[] { "Parent", "DataContext" };
        return skip.Contains(prop.Name);
    }
}
