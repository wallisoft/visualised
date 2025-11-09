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
        else if (prop.PropertyType.IsEnum)
            row.Children.Add(CreateTinyCombo(control, prop));
        else
            row.Children.Add(new TextBlock { Text = "(complex)", FontSize = 11 });
        
        panel.Children.Add(row);
    }
    
    private Control CreateTinyTextBox(Control control, PropertyInfo prop)
    {
        // Label that looks like textbox
        var fakeTextBox = new Label
        {
            Content = prop.GetValue(control)?.ToString() ?? "",
            Width = 120,
            MinHeight = 19,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(4, 2, 4, 2),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(1),
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
                prop.SetValue(control, realTextBox.Text);
                
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
        var container = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 1 };
        
        // Fake textbox showing current value
        var fakeTextBox = new Label
        {
            Content = prop.GetValue(control)?.ToString() ?? "",
            Width = 120,
            MinHeight = 19,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(4, 2, 4, 2),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        // @ button
        var dropBtn = new Button
        {
            Content = "@",
            Width = 19,
            Height = 19,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(0),
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(1)
        };
        
        dropBtn.Click += (s, e) =>
        {
            // Show multi-line textbox with all enum values
            var multiLine = new TextBox
            {
                Width = 138,
                MinHeight = 100,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };
            
            // Fill with enum values
            var values = string.Join("\n", Enum.GetNames(prop.PropertyType));
            multiLine.Text = values;
            
            // Would need SwapControls and selection logic here
        };
        
        container.Children.Add(fakeTextBox);
        container.Children.Add(dropBtn);
        
        return container;
    }
    
    private bool ShouldSkip(PropertyInfo prop)
    {
        var skip = new[] { "Parent", "DataContext", "Resources", "Styles", "Classes", "CommandBindings" };
        return skip.Contains(prop.Name) || 
               prop.PropertyType.IsGenericType ||
               prop.PropertyType.IsArray;
    }
}
