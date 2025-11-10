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
            var priority = new[] { "Margin", "FontSize", "Background", "Width", "Height", "Name", "Content", "Text" };
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
        // Load TinyTextBox template from database
        var vmlControls = VmlLoader.LoadFromDatabase("TinyTextBox");
        if (vmlControls.Count > 0)
        {
            var builder = new ControlBuilder(vmlControls);
            var controls = builder.BuildControls();
            if (controls.Count > 0)
            {
                var tinyTextBox = controls[0];
                
                // Set initial value on fakeBox
                if (tinyTextBox is Panel panel)
                {
                    var fakeBox = panel.Children.OfType<Label>().FirstOrDefault();
                    if (fakeBox != null)
                    {
                        fakeBox.Content = prop.GetValue(control)?.ToString() ?? "";
                    }
                }
                
                return tinyTextBox;
            }
        }
        
        // Fallback to simple textbox if loading fails

        // Label that looks like textbox
        var fakeTextBox = new Label
        {
            Content = prop.GetValue(control)?.ToString() ?? "",
            Width = 120,
            MinHeight = 15,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(4, 2, 4, 2),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        fakeTextBox.PointerPressed += (s, e) =>
        {
            var parent = fakeTextBox.Parent as Panel;
            if (parent == null) return;
            
            var realTextBox = new TextBox
            {
                Text = fakeTextBox.Content?.ToString() ?? "",
                Width = 120,
                MinHeight = 17
            };
            
            realTextBox.LostFocus += (s2, e2) =>
            {
                fakeTextBox.Content = realTextBox.Text;
                
                // Convert to correct type
                object value = realTextBox.Text;
                if (prop.PropertyType == typeof(double))
                {
                    if (double.TryParse(realTextBox.Text, out var d))
                        value = d;
                }
                else if (prop.PropertyType == typeof(int))
                {
                    if (int.TryParse(realTextBox.Text, out var i))
                        value = i;
                }
                
                prop.SetValue(control, value);
                
                // Swap back
                var idx = parent.Children.IndexOf(realTextBox);
                parent.Children.RemoveAt(idx);
                parent.Children.Insert(idx, fakeTextBox);
            };
            
            // Swap fake for real
            var index = parent.Children.IndexOf(fakeTextBox);
            parent.Children.RemoveAt(index);
            parent.Children.Insert(index, realTextBox);
            realTextBox.Focus();
        };
        
        return fakeTextBox;
    }
    
    private Control CreateTinyCombo(Control control, PropertyInfo prop)
    {
        Console.WriteLine($"[COMBO] Loading TinyCombo for {prop.Name} ({prop.PropertyType.Name})");
        // Load TinyCombo from database
        var vmlControls = VmlLoader.LoadFromDatabase("TinyCombo");
        Console.WriteLine($"[COMBO] Loaded {vmlControls.Count} controls from database");
        if (vmlControls.Count > 0)
        {
            var builder = new ControlBuilder(vmlControls);
            var controls = builder.BuildControls();
            if (controls.Count > 0 && controls[0] is Panel container)
            {
                var valueBox = container.Children.OfType<Label>().FirstOrDefault();
                var dropBtn = container.Children.OfType<Button>().FirstOrDefault();
                
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
        var skip = new[] { "Parent", "DataContext", "Resources", "Styles", "Classes", "CommandBindings" };
        return skip.Contains(prop.Name) || 
               prop.PropertyType.IsGenericType ||
               prop.PropertyType.IsArray;
    }
}
