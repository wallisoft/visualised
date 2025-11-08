using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VB;

public class PropertiesPanel
{
    private StackPanel panel;
    private Control? selectedControl;
    private Dictionary<string, Control> propertyControls = new();
    
    public PropertiesPanel(StackPanel targetPanel)
    {
        panel = targetPanel;
    }
    
    public void ShowPropertiesFor(Control control)
    {
        selectedControl = control;
        panel.Children.Clear();
        propertyControls.Clear();
        
        if (control == null) return;
        
        var header = new TextBlock 
        { 
            Text = $"{control.GetType().Name}",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };
        panel.Children.Add(header);
        
        var props = control.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetSetMethod()?.IsPublic == true)
            .OrderBy(p => p.Name)
            .ToList();
        
        var groups = new Dictionary<string, List<PropertyInfo>>
        {
            ["Common"] = new List<PropertyInfo>(),
            ["Layout"] = new List<PropertyInfo>(),
            ["Appear"] = new List<PropertyInfo>(),
            ["Behav"] = new List<PropertyInfo>(),
            ["Custom"] = new List<PropertyInfo>(),
            ["Adv"] = new List<PropertyInfo>()
        };
        
        foreach (var prop in props)
        {
            var name = prop.Name;
            
            if (name == "ZIndex" || name == "BuildOrder" || name == "Tag")
                groups["Custom"].Add(prop);
            else if (new[] { 
                "Name",           // Identity
                "Content", "Text", // Content  
                "Width", "Height", // Dimensions
                "Margin", "Padding", // Spacing
                "HorizontalAlignment", "VerticalAlignment", // Alignment
                "Background", "Foreground", // Appearance
                "FontSize", "FontWeight", // Text Style
                "IsVisible", "IsEnabled" // State
            }.Contains(name))
                groups["Common"].Add(prop);
            else if (name.Contains("Width") || name.Contains("Height") || name.Contains("Margin") || 
                     name.Contains("Padding") || name.Contains("Alignment") || name.Contains("Stretch"))
                groups["Layout"].Add(prop);
            else if (name.Contains("Color") || name.Contains("Background") || name.Contains("Foreground") ||
                     name.Contains("Border") || name.Contains("Font") || name.Contains("Opacity"))
                groups["Appear"].Add(prop);
            else if (name.Contains("Click") || name.Contains("Focus") || name.Contains("IsReadOnly") ||
                     name.Contains("IsChecked") || name.Contains("Selected"))
                groups["Behav"].Add(prop);
            else
                groups["Adv"].Add(prop);
        }
        
        foreach (var group in groups)
        {
            if (group.Value.Count == 0) continue;
            
            var expander = new Expander
            {
                Header = group.Key,
                IsExpanded = group.Key == "Common" || group.Key == "Custom",
                Margin = new Avalonia.Thickness(0, 3, 0, 3),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            
            var groupPanel = new StackPanel { Spacing = 5, Margin = new Avalonia.Thickness(5, 3, 0, 3) };
            
            foreach (var prop in group.Value)
            {
                // Special handling: combine IsEnabled and IsVisible on one line
                if (prop.Name == "IsEnabled" && group.Value.Any(p => p.Name == "IsVisible"))
                {
                    var enabledProp = prop;
                    var visibleProp = group.Value.First(p => p.Name == "IsVisible");
                    var checkRow = CreateCheckboxRow(control, enabledProp, visibleProp);
                    if (checkRow != null) groupPanel.Children.Add(checkRow);
                    continue;
                }
                if (prop.Name == "IsVisible") continue; // Already handled with IsEnabled
                
                var propControl = CreatePropertyControl(control, prop);
                if (propControl != null)
                {
                    groupPanel.Children.Add(propControl);
                }
            }
            
            expander.Content = groupPanel;
            panel.Children.Add(expander);
        }
        
        Console.WriteLine($"[PROPS] {props.Count} props for {control.GetType().Name}");
    }
    
    private Control? CreateCheckboxRow(Control target, PropertyInfo enabledProp, PropertyInfo visibleProp)
    {
        try
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            
            var enabledValue = (bool?)enabledProp.GetValue(target);
            var enabledCheck = new CheckBox 
            { 
                Content = "En",
                IsChecked = enabledValue,
                Margin = new Avalonia.Thickness(0, 0, 8, 0)
            };
            ToolTip.SetTip(enabledCheck, "IsEnabled");
            enabledCheck.Click += (s, e) => SetProperty(target, enabledProp, enabledCheck.IsChecked);
            row.Children.Add(enabledCheck);
            
            var visibleValue = (bool?)visibleProp.GetValue(target);
            var visibleCheck = new CheckBox 
            { 
                Content = "Vis",
                IsChecked = visibleValue
            };
            ToolTip.SetTip(visibleCheck, "IsVisible");
            visibleCheck.Click += (s, e) => SetProperty(target, visibleProp, visibleCheck.IsChecked);
            row.Children.Add(visibleCheck);
            
            return row;
        }
        catch
        {
            return null;
        }
    }
    
    private Control? CreatePropertyControl(Control target, PropertyInfo prop)
    {
        var propType = prop.PropertyType;
        var propName = prop.Name;
        
        try
        {
            var currentValue = prop.GetValue(target);
            
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            
            var displayName = propName.Length > 8 ? propName.Substring(0, 8) + "..." : propName;
            var label = new TextBlock 
            { 
                Text = displayName + ":",
                Width = 50,
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTip.SetTip(label, propName);
            row.Children.Add(label);
            
            Control? editor = null;
            
            if (propType == typeof(bool) || propType == typeof(bool?))
            {
                var check = new CheckBox { IsChecked = (bool?)currentValue };
                check.Click += (s, e) => SetProperty(target, prop, check.IsChecked);
                editor = check;
            }
            else if (propType == typeof(int) || propType == typeof(double) || 
                     propType == typeof(int?) || propType == typeof(double?))
            {
                // Use TextBox for cleaner, more compact UI
                var numBox = new TextBox 
                { 
                    Text = currentValue?.ToString() ?? "0",
                    Width = 50,
                    Padding = new Avalonia.Thickness(2)
                };
                
                numBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        try
                        {
                            if (propType == typeof(int) || propType == typeof(int?))
                                SetProperty(target, prop, int.Parse(numBox.Text ?? "0"));
                            else
                                SetProperty(target, prop, double.Parse(numBox.Text ?? "0"));
                        }
                        catch { }
                        FocusNextProperty(numBox);
                        e.Handled = true;
                    }
                };
                
                numBox.LostFocus += (s, e) =>
                {
                    try
                    {
                        if (propType == typeof(int) || propType == typeof(int?))
                            SetProperty(target, prop, int.Parse(numBox.Text ?? "0"));
                        else
                            SetProperty(target, prop, double.Parse(numBox.Text ?? "0"));
                    }
                    catch { }
                };
                
                editor = numBox;
            }
            else if (propType == typeof(string))
            {
                var textBox = new TextBox 
                { 
                    Text = currentValue?.ToString() ?? "",
                    Width = 60,
                    Padding = new Avalonia.Thickness(2),
                };
                
                // Enter key = save and focus next
                textBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        if (propName == "Name")
                        {
                            SetProperty(target, prop, textBox.Text);
                            var contentProp = target.GetType().GetProperty("Content");
                            if (contentProp != null && contentProp.CanWrite)
                                SetProperty(target, contentProp, textBox.Text);
                            var textProp = target.GetType().GetProperty("Text");
                            if (textProp != null && textProp.CanWrite)
                                SetProperty(target, textProp, textBox.Text);
                        }
                        else
                        {
                            SetProperty(target, prop, textBox.Text);
                        }
                        FocusNextProperty(textBox);
                        e.Handled = true;
                    }
                };
                
                // LostFocus = save
                textBox.LostFocus += (s, e) =>
                {
                    if (propName == "Name")
                    {
                        SetProperty(target, prop, textBox.Text);
                        var contentProp = target.GetType().GetProperty("Content");
                        if (contentProp != null && contentProp.CanWrite)
                            SetProperty(target, contentProp, textBox.Text);
                        var textProp = target.GetType().GetProperty("Text");
                        if (textProp != null && textProp.CanWrite)
                            SetProperty(target, textProp, textBox.Text);
                    }
                    else
                    {
                        SetProperty(target, prop, textBox.Text);
                    }
                };
                
                editor = textBox;
            }
            else if (propType.IsEnum)
            {
                var combo = new ComboBox { Width = 60 };
                foreach (var val in Enum.GetValues(propType))
                    combo.Items.Add(val);
                combo.SelectedItem = currentValue;
                combo.SelectionChanged += (s, e) => SetProperty(target, prop, combo.SelectedItem);
                editor = combo;
            }
            else if (propType == typeof(Avalonia.Thickness) || propType == typeof(Avalonia.Thickness?))
            {
                var thickness = (Avalonia.Thickness?)currentValue ?? new Avalonia.Thickness(0);
                var thickBox = new TextBox 
                { 
                    Text = $"{thickness.Left},{thickness.Top},{thickness.Right},{thickness.Bottom}",
                    Width = 60,
                    Watermark = "L,T,R,B"
                };
                thickBox.LostFocus += (s, e) =>
                {
                    try
                    {
                        var parts = thickBox.Text?.Split(',');
                        if (parts?.Length == 4)
                        {
                            var t = new Avalonia.Thickness(
                                double.Parse(parts[0]), double.Parse(parts[1]),
                                double.Parse(parts[2]), double.Parse(parts[3]));
                            SetProperty(target, prop, t);
                        }
                    }
                    catch { }
                };
                editor = thickBox;
            }
            else if (propType == typeof(IBrush) || propType.Name.Contains("Brush"))
            {
                var colorBox = new TextBox 
                { 
                    Text = currentValue?.ToString() ?? "#FFF",
                    Width = 50,
                    Watermark = "#RGB"
                };
                colorBox.LostFocus += (s, e) =>
                {
                    try
                    {
                        var brush = new SolidColorBrush(Color.Parse(colorBox.Text ?? "#FFF"));
                        SetProperty(target, prop, brush);
                    }
                    catch { }
                };
                editor = colorBox;
            }
            else if (propName.Contains("Horizontal") || propName.Contains("Vertical"))
            {
                var display = new TextBlock 
                { 
                    Text = currentValue?.ToString()?.Replace("Horizontal", "H").Replace("Vertical", "V") ?? "-",
                    Foreground = new SolidColorBrush(Color.Parse("#666")),
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                };
                editor = display;
            }
            else
            {
                var display = new TextBlock 
                { 
                    Text = currentValue?.ToString() ?? "-",
                    Foreground = new SolidColorBrush(Color.Parse("#999")),
                    FontSize = 11,
                    FontStyle = FontStyle.Italic,
                    VerticalAlignment = VerticalAlignment.Center,
                    MaxWidth = 60,
                    Padding = new Avalonia.Thickness(2),
                    TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                };
                editor = display;
            }
            
            if (editor != null)
            {
                row.Children.Add(editor);
                propertyControls[propName] = editor;
            }
            
            return row;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROPS] Error: {propName}: {ex.Message}");
            return null;
        }
    }
    
    private void FocusNextProperty(Control current)
    {
        var allControls = propertyControls.Values.ToList();
        var currentIndex = allControls.IndexOf(current);
        
        if (currentIndex >= 0)
        {
            var nextIndex = (currentIndex + 1) % allControls.Count;
            var next = allControls[nextIndex];
            
            if (next is TextBox tb)
                tb.Focus();
            else if (next is NumericUpDown num)
                num.Focus();
        }
    }
    
    private void SetProperty(Control target, PropertyInfo prop, object? value)
    {
        try
        {
            prop.SetValue(target, value);
            Console.WriteLine($"[PROPS] {prop.Name} = {value}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROPS] Error setting {prop.Name}: {ex.Message}");
        }
    }
}






