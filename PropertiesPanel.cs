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
                "Name", "Content", "Text", "Width", "Height", "Margin", "Padding",
                "HorizontalAlignment", "VerticalAlignment", "Background", "Foreground",
                "BorderBrush", "BorderThickness", "CornerRadius", "FontSize", "FontWeight",
                "IsVisible", "IsEnabled"
            }.Contains(name))
            {
                // Skip - handled by BuildCommonPropertiesPanel
            }
            else if (name.Contains("Width") || name.Contains("Height") || name.Contains("Margin") ||
                     name.Contains("Padding") || name.Contains("Alignment") || name.Contains("Stretch"))
                groups["Layout"].Add(prop);
            else if (name.Contains("Color") || name.Contains("Background") || name.Contains("Foreground") ||
                     name.Contains("Border") || name.Contains("Font") || name.Contains("Opacity"))
                groups["Appear"].Add(prop);
            else if (name.Contains("Click") || name.Contains("Focus") || name.Contains("Key") ||
                     name.Contains("Mouse") || name.Contains("Pointer") || name.Contains("IsEnabled") ||
                     name.Contains("IsVisible"))
                groups["Behav"].Add(prop);
            else
                groups["Adv"].Add(prop);
        }

        // Render Common properties with custom controls
        BuildCommonPropertiesPanel(control, panel);
        
        // Render all other properties as flat list
        foreach (var group in groups)
        {
            foreach (var prop in group.Value)
            {
                var propControl = CreatePropertyControl(control, prop);
                if (propControl != null)
                    panel.Children.Add(propControl);
            }
        }
        
                Console.WriteLine($"[PROPS] {props.Count} props for {control.GetType().Name}");
    }

    private void AddCommonTextProperty(Control control, StackPanel groupPanel, string label, PropertyInfo prop, int width)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(new TextBlock { Text = label, Width = 50, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        var currentValue = prop.GetValue(control);
        var btn = CreateTinyTextBoxButton(currentValue?.ToString() ?? "", width);
        
        btn.Click += (s, e) =>
        {
            var editBox = CreateEditTextBox(btn.Content?.ToString() ?? "", width);
            editBox.LostFocus += (s2, e2) =>
            {
                btn.Content = editBox.Text;
                prop.SetValue(control, editBox.Text);
                SwapControls(editBox, btn);
            };
            editBox.KeyDown += (s2, e2) => { if (e2.Key == Key.Enter) { FocusNextProperty(btn); e2.Handled = true; } };
            SwapControls(btn, editBox);
            editBox.Focus();
        };
        
        row.Children.Add(btn);
        groupPanel.Children.Add(row);
    }

    private void AddCommonSizeProperty(Control control, StackPanel groupPanel, PropertyInfo widthProp, PropertyInfo heightProp)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(new TextBlock { Text = "Size:", Width = 50, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        // Width button
        var widthValue = widthProp.GetValue(control);
        var widthInt = widthValue != null ? (int)Math.Round(Convert.ToDouble(widthValue)) : 0;
        var widthBtn = CreateTinyTextBoxButton(widthInt.ToString(), 40);
        
        widthBtn.Click += (s, e) =>
        {
            var editBox = CreateEditTextBox(widthBtn.Content?.ToString() ?? "0", 40);
            editBox.LostFocus += (s2, e2) =>
            {
                if (int.TryParse(editBox.Text, out int val))
                {
                    widthBtn.Content = val.ToString();
                    widthProp.SetValue(control, (double)val);
                }
                SwapControls(editBox, widthBtn);
            };
            editBox.KeyDown += (s2, e2) => { if (e2.Key == Key.Enter) { FocusNextProperty(widthBtn); e2.Handled = true; } };
            SwapControls(widthBtn, editBox);
            editBox.Focus();
        };
        
        // Height button
        var heightValue = heightProp.GetValue(control);
        var heightInt = heightValue != null ? (int)Math.Round(Convert.ToDouble(heightValue)) : 0;
        var heightBtn = CreateTinyTextBoxButton(heightInt.ToString(), 40);
        
        heightBtn.Click += (s, e) =>
        {
            var editBox = CreateEditTextBox(heightBtn.Content?.ToString() ?? "0", 40);
            editBox.LostFocus += (s2, e2) =>
            {
                if (int.TryParse(editBox.Text, out int val))
                {
                    heightBtn.Content = val.ToString();
                    heightProp.SetValue(control, (double)val);
                }
                SwapControls(editBox, heightBtn);
            };
            editBox.KeyDown += (s2, e2) => { if (e2.Key == Key.Enter) { FocusNextProperty(heightBtn); e2.Handled = true; } };
            SwapControls(heightBtn, editBox);
            editBox.Focus();
        };
        
        row.Children.Add(widthBtn);
        row.Children.Add(heightBtn);
        groupPanel.Children.Add(row);
    }

    private Button CreateTinyTextBoxButton(string content, int width)
    {
        return new Button
        {
            Content = content,
            Width = width,
            Height = 18,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Padding = new Avalonia.Thickness(2, 1, 2, 3),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Avalonia.Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Left
        };
    }

    private TextBox CreateEditTextBox(string text, int width)
    {
        return new TextBox
        {
            Text = text,
            Width = width,
            Height = 18,
            FontSize = 11,
            Padding = new Avalonia.Thickness(2, 1, 2, 3),
            BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
            BorderThickness = new Avalonia.Thickness(2)
        };
    }

    private void SwapControls(Control from, Control to)
    {
        var parent = from.Parent as Panel;
        if (parent != null)
        {
            var idx = parent.Children.IndexOf(from);
            parent.Children.RemoveAt(idx);
            parent.Children.Insert(idx, to);
        }
    }

    private void AddCommonNumericProperty(Control control, StackPanel groupPanel, string label, PropertyInfo prop, int width)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(new TextBlock { Text = label, Width = 50, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        var currentValue = prop.GetValue(control);
        var intValue = currentValue != null ? (int)Math.Round(Convert.ToDouble(currentValue)) : 0;
        var btn = CreateTinyTextBoxButton(intValue.ToString(), width);
        
        btn.Click += (s, e) =>
        {
            var editBox = CreateEditTextBox(btn.Content?.ToString() ?? "0", width);
            editBox.LostFocus += (s2, e2) =>
            {
                if (int.TryParse(editBox.Text, out int val))
                {
                    btn.Content = val.ToString();
                    prop.SetValue(control, (double)val);
                }
                SwapControls(editBox, btn);
            };
            editBox.KeyDown += (s2, e2) => { if (e2.Key == Key.Enter) { FocusNextProperty(btn); e2.Handled = true; } };
            SwapControls(btn, editBox);
            editBox.Focus();
        };
        
        row.Children.Add(btn);
        groupPanel.Children.Add(row);
    }

    private void AddCommonThicknessProperty(Control control, StackPanel groupPanel, string label, PropertyInfo prop)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(new TextBlock { Text = label, Width = 50, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        var thickness = (Avalonia.Thickness?)prop.GetValue(control) ?? new Avalonia.Thickness(0);
        var btn = CreateTinyTextBoxButton($"{(int)thickness.Left},{(int)thickness.Top},{(int)thickness.Right},{(int)thickness.Bottom}", 80);
        
        btn.Click += (s, e) =>
        {
            var editBox = CreateEditTextBox(btn.Content?.ToString() ?? "0,0,0,0", 80);
            editBox.LostFocus += (s2, e2) =>
            {
                try
                {
                    var parts = editBox.Text?.Split(',');
                    if (parts?.Length == 4)
                    {
                        var t = new Avalonia.Thickness(
                            int.Parse(parts[0]), int.Parse(parts[1]),
                            int.Parse(parts[2]), int.Parse(parts[3]));
                        btn.Content = $"{(int)t.Left},{(int)t.Top},{(int)t.Right},{(int)t.Bottom}";
                        prop.SetValue(control, t);
                    }
                }
                catch { }
                SwapControls(editBox, btn);
            };
            editBox.KeyDown += (s2, e2) => { if (e2.Key == Key.Enter) { FocusNextProperty(btn); e2.Handled = true; } };
            SwapControls(btn, editBox);
            editBox.Focus();
        };
        
        row.Children.Add(btn);
        groupPanel.Children.Add(row);
    }

    private void AddCommonCornerProperty(Control control, StackPanel groupPanel, PropertyInfo prop)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(new TextBlock { Text = "Corner:", Width = 50, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        var cornerRadius = prop.GetValue(control) as Avalonia.CornerRadius? ?? new Avalonia.CornerRadius(0);
        var intValue = (int)cornerRadius.TopLeft;
        var btn = CreateTinyTextBoxButton(intValue.ToString(), 40);
        
        btn.Click += (s, e) =>
        {
            var editBox = CreateEditTextBox(btn.Content?.ToString() ?? "0", 40);
            editBox.LostFocus += (s2, e2) =>
            {
                if (int.TryParse(editBox.Text, out int val))
                {
                    btn.Content = val.ToString();
                    prop.SetValue(control, new Avalonia.CornerRadius(val));
                }
                SwapControls(editBox, btn);
            };
            editBox.KeyDown += (s2, e2) => { if (e2.Key == Key.Enter) { FocusNextProperty(btn); e2.Handled = true; } };
            SwapControls(btn, editBox);
            editBox.Focus();
        };
        
        row.Children.Add(btn);
        groupPanel.Children.Add(row);
    }


    private void AddCommonMultilineProperty(Control control, StackPanel groupPanel, string label, PropertyInfo prop, int width)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(new TextBlock { Text = label, Width = 50, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        var currentValue = prop.GetValue(control);
        var btn = CreateTinyTextBoxButton(currentValue?.ToString() ?? "", width);
        
        btn.Click += (s, e) =>
        {
            var editBox = new TextBox
            {
                Text = btn.Content?.ToString() ?? "",
                Width = width,
                Height = 60,
                FontSize = 11,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Avalonia.Thickness(2, 1, 2, 3),
                BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
                BorderThickness = new Avalonia.Thickness(2)
            };
            
            editBox.LostFocus += (s2, e2) =>
            {
                btn.Content = editBox.Text;
                prop.SetValue(control, editBox.Text);
                SwapControls(editBox, btn);
            };
            
            editBox.KeyDown += (s2, e2) =>
            {
                if (e2.Key == Key.Enter && !e2.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    FocusNextProperty(btn);
                    e2.Handled = true;
                }
            };
            
            SwapControls(btn, editBox);
            editBox.Focus();
        };
        
        row.Children.Add(btn);
        groupPanel.Children.Add(row);
    }


    private void AddCommonEnumProperty(Control control, StackPanel groupPanel, string label, PropertyInfo prop)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(new TextBlock { Text = label, Width = 50, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        var currentValue = prop.GetValue(control);
        var valueBtn = CreateTinyTextBoxButton(currentValue?.ToString() ?? "", 60);
        
        var dropBtn = new Button
        {
            Content = "@",
            Width = 17,
            Height = 18,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Padding = new Avalonia.Thickness(0),
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.Parse("#2196F3")),
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Avalonia.Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        var container = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 1 };
        container.Children.Add(valueBtn);
        container.Children.Add(dropBtn);
        
        EventHandler<Avalonia.Interactivity.RoutedEventArgs> showDropdown = (s, e) =>
        {
            var combo = new ComboBox
            {
                Width = 78,
                Height = 18,
                FontSize = 11,
                BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
                BorderThickness = new Avalonia.Thickness(2)
            };
            
            foreach (var val in Enum.GetValues(prop.PropertyType))
                combo.Items.Add(val);
            combo.SelectedItem = currentValue;
            
            combo.SelectionChanged += (s2, e2) =>
            {
                if (combo.SelectedItem != null)
                {
                    valueBtn.Content = combo.SelectedItem.ToString();
                    prop.SetValue(control, combo.SelectedItem);
                    SwapControls(combo, container);
                }
            };
            
            combo.DropDownClosed += (s2, e2) => SwapControls(combo, container);
            
            SwapControls(container, combo);
            combo.Focus();
            Avalonia.Threading.Dispatcher.UIThread.Post(() => combo.IsDropDownOpen = true, Avalonia.Threading.DispatcherPriority.Background);
        };
        
        valueBtn.Click += showDropdown;
        dropBtn.Click += showDropdown;
        
        row.Children.Add(container);
        groupPanel.Children.Add(row);
    }

    private void AddCommonColorProperty(Control control, StackPanel groupPanel, string label, PropertyInfo prop)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(new TextBlock { Text = label, Width = 50, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        var currentValue = prop.GetValue(control);
        var colorText = currentValue?.ToString() ?? "#FFF";
        var btn = CreateTinyTextBoxButton(colorText, 60);
        
        btn.Click += (s, e) =>
        {
            var editBox = CreateEditTextBox(btn.Content?.ToString() ?? "#FFF", 60);
            editBox.LostFocus += (s2, e2) =>
            {
                try
                {
                    var brush = new SolidColorBrush(Color.Parse(editBox.Text ?? "#FFF"));
                    btn.Content = editBox.Text;
                    prop.SetValue(control, brush);
                }
                catch { }
                SwapControls(editBox, btn);
            };
            editBox.KeyDown += (s2, e2) => { if (e2.Key == Key.Enter) { FocusNextProperty(btn); e2.Handled = true; } };
            SwapControls(btn, editBox);
            editBox.Focus();
        };
        
        row.Children.Add(btn);
        
        // @ button for color picker
        var pickBtn = new Button
        {
            Content = "@",
            Width = 17,
            Height = 18,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Padding = new Avalonia.Thickness(0),
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.Parse("#2196F3")),
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Avalonia.Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        pickBtn.Click += async (s, e) =>
        {
            // TODO: Open color picker dialog
            // For now just cycle through common colors
            var colors = new[] { "#FFFFFF", "#000000", "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF" };
            var currentColor = btn.Content?.ToString() ?? "#FFF";
            var idx = Array.IndexOf(colors, currentColor.ToUpper());
            var nextColor = colors[(idx + 1) % colors.Length];
            btn.Content = nextColor;
            prop.SetValue(control, new SolidColorBrush(Color.Parse(nextColor)));
        };
        
        row.Children.Add(pickBtn);
        groupPanel.Children.Add(row);
    }


    private void AddCommonFontProperty(Control control, StackPanel groupPanel, PropertyInfo fontSizeProp)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(new TextBlock { Text = "Font:", Width = 50, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        var currentSize = fontSizeProp.GetValue(control);
        var intSize = currentSize != null ? (int)Math.Round(Convert.ToDouble(currentSize)) : 12;
        var btn = CreateTinyTextBoxButton(intSize.ToString(), 40);
        
        btn.Click += (s, e) =>
        {
            var editBox = CreateEditTextBox(btn.Content?.ToString() ?? "12", 40);
            editBox.LostFocus += (s2, e2) =>
            {
                if (int.TryParse(editBox.Text, out int val))
                {
                    btn.Content = val.ToString();
                    fontSizeProp.SetValue(control, (double)val);
                }
                SwapControls(editBox, btn);
            };
            editBox.KeyDown += (s2, e2) => { if (e2.Key == Key.Enter) { FocusNextProperty(btn); e2.Handled = true; } };
            SwapControls(btn, editBox);
            editBox.Focus();
        };
        
        row.Children.Add(btn);
        
        // @ button for font dialog
        var fontBtn = new Button
        {
            Content = "@",
            Width = 17,
            Height = 18,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Padding = new Avalonia.Thickness(0),
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.Parse("#2196F3")),
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Avalonia.Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        fontBtn.Click += async (s, e) =>
        {
            // TODO: Open font dialog with FontFamily, Bold, Italic, Size options
            // For now just cycle through common sizes
            var sizes = new[] { 8, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32 };
            var currentVal = int.Parse(btn.Content?.ToString() ?? "12");
            var idx = Array.IndexOf(sizes, currentVal);
            var nextSize = sizes[(idx + 1) % sizes.Length];
            btn.Content = nextSize.ToString();
            fontSizeProp.SetValue(control, (double)nextSize);
        };
        
        row.Children.Add(fontBtn);
        groupPanel.Children.Add(row);
    }


    private void AddCommonBoolPair(Control control, StackPanel groupPanel, PropertyInfo visibleProp, PropertyInfo enabledProp)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(new TextBlock { Text = "Visible:", Width = 50, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        // Visible checkbox
        bool isVisible = (bool?)visibleProp.GetValue(control) ?? true;
        var visBtn = new Button
        {
            Content = isVisible ? "✓" : "✗",
            Width = 18,
            Height = 18,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Padding = new Avalonia.Thickness(0),
            Background = Brushes.White,
            Foreground = isVisible ? new SolidColorBrush(Color.Parse("#66bb6a")) : new SolidColorBrush(Color.Parse("#ff0000")),
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Avalonia.Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        visBtn.Click += (s, e) =>
        {
            isVisible = !isVisible;
            visBtn.Content = isVisible ? "✓" : "✗";
            visBtn.Foreground = isVisible ? new SolidColorBrush(Color.Parse("#66bb6a")) : new SolidColorBrush(Color.Parse("#ff0000"));
            visibleProp.SetValue(control, isVisible);
        };
        
        row.Children.Add(visBtn);
        
        // Enabled label and checkbox
        row.Children.Add(new TextBlock { Text = "Enabled:", Width = 47, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
        
        bool isEnabled = (bool?)enabledProp.GetValue(control) ?? true;
        var enBtn = new Button
        {
            Content = isEnabled ? "✓" : "✗",
            Width = 18,
            Height = 18,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Padding = new Avalonia.Thickness(0),
            Background = Brushes.White,
            Foreground = isEnabled ? new SolidColorBrush(Color.Parse("#66bb6a")) : new SolidColorBrush(Color.Parse("#ff0000")),
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Avalonia.Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        enBtn.Click += (s, e) =>
        {
            isEnabled = !isEnabled;
            enBtn.Content = isEnabled ? "✓" : "✗";
            enBtn.Foreground = isEnabled ? new SolidColorBrush(Color.Parse("#66bb6a")) : new SolidColorBrush(Color.Parse("#ff0000"));
            enabledProp.SetValue(control, isEnabled);
        };
        
        row.Children.Add(enBtn);
        groupPanel.Children.Add(row);
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
                FontSize = 11,
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
                FontSize = 11,
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
    
    private void BuildCommonPropertiesPanel(Control control, StackPanel groupPanel)
    {
        // Name
        var nameProp = control.GetType().GetProperty("Name");
        if (nameProp != null) 
            AddCommonTextProperty(control, groupPanel, "Name:", nameProp, 120);
        
        // Content
        var contentProp = control.GetType().GetProperty("Content");
        if (contentProp != null && contentProp.CanWrite)
        {
            var contentVal = contentProp.GetValue(control);
            if (contentVal == null || contentVal is string)
                AddCommonTextProperty(control, groupPanel, "Content:", contentProp, 120);
        }
        
        // Text
        var textProp = control.GetType().GetProperty("Text");
        if (textProp != null) 
            AddCommonTextProperty(control, groupPanel, "Text:", textProp, 120);
        
        // Size: Width + Height side by side
        var widthProp = control.GetType().GetProperty("Width");
        var heightProp = control.GetType().GetProperty("Height");
        if (widthProp != null && heightProp != null)
            AddCommonSizeProperty(control, groupPanel, widthProp, heightProp);
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
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTip.SetTip(label, propName);
            row.Children.Add(label);
            
            Control? editor = null;
            
            if (propType == typeof(bool) || propType == typeof(bool?))
            {
                bool isChecked = (bool?)currentValue ?? false;
                var btn = new Button
                {
                    Content = isChecked ? "✓" : "✗",
                    Width = 18,
                    Height = 18,
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Padding = new Avalonia.Thickness(0),
                    Background = Brushes.White,
                    Foreground = isChecked ? new SolidColorBrush(Color.Parse("#66bb6a")) : Brushes.Black,
                    BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
                    BorderThickness = new Avalonia.Thickness(1),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                
                btn.Click += (s, e) =>
                {
                    isChecked = !isChecked;
                    btn.Content = isChecked ? "✓" : "✗";
                    btn.Foreground = isChecked ? new SolidColorBrush(Color.Parse("#66bb6a")) : Brushes.Black;
                    SetProperty(target, prop, isChecked);
                };
                
                editor = btn;
            }
            else if (propType == typeof(int) || propType == typeof(double) || 
                     propType == typeof(int?) || propType == typeof(double?))
            {
                var btn = new Button
                {
                    Content = currentValue?.ToString() ?? "0",
                    Width = 55,
                    Height = 18,
                    FontSize = 11,
                    FontWeight = FontWeight.Bold,
                    Padding = new Avalonia.Thickness(2, 1, 2, 3),
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
                    BorderThickness = new Avalonia.Thickness(1),
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                
                // Click handler - spawn overlay TextBox for editing
                btn.Click += (s, e) =>
                {
                    var editBox = new TextBox
                    {
                        Text = btn.Content?.ToString() ?? "0",
                        Width = 55,
                        Height = 18,
                        FontSize = 11,
                        Padding = new Avalonia.Thickness(2, 1, 2, 3),
                        BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
                        BorderThickness = new Avalonia.Thickness(2)
                    };
                    
                    editBox.LostFocus += (s2, e2) =>
                    {
                        // Update button
                        btn.Content = editBox.Text;
                        
                        // Update property
                        try
                        {
                            if (propType == typeof(int) || propType == typeof(int?))
                                SetProperty(target, prop, int.Parse(editBox.Text ?? "0"));
                            else
                                SetProperty(target, prop, double.Parse(editBox.Text ?? "0"));
                        }
                        catch { }
                        
                        // Remove TextBox from panel
                        var parent = editBox.Parent as Panel;
                        if (parent != null)
                        {
                            var idx = parent.Children.IndexOf(editBox);
                            parent.Children.RemoveAt(idx);
                            parent.Children.Insert(idx, btn);
                        }
                    };
                    
                    editBox.KeyDown += (s2, e2) =>
                    {
                        if (e2.Key == Key.Enter)
                        {
                            FocusNextProperty(btn);
                            e2.Handled = true;
                        }
                    };
                    
                    // Swap button for TextBox
                    var parent = btn.Parent as Panel;
                    if (parent != null)
                    {
                        var idx = parent.Children.IndexOf(btn);
                        parent.Children.RemoveAt(idx);
                        parent.Children.Insert(idx, editBox);
                        editBox.Focus();
                    }
                };
                
                editor = btn;
            }
            else if (propType == typeof(string))
            {
                var textBox = new TextBox 
                { 
                    Text = currentValue?.ToString() ?? "",
                    Width = 120,
                    FontSize = 11,
                    Padding = new Avalonia.Thickness(3, 2, 3, 2),
                    BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
                    BorderThickness = new Avalonia.Thickness(1)
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
                var valueBtn = new Button
                {
                    Content = currentValue?.ToString() ?? "",
                    Width = 60,
                    Height = 18,
                    FontSize = 11,
                    FontWeight = FontWeight.Bold,
                    Padding = new Avalonia.Thickness(2, 1, 2, 3),
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
                    BorderThickness = new Avalonia.Thickness(1),
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                
                var dropBtn = new Button
                {
                    Content = "@",
                    Width = 17,
                    Height = 18,
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Padding = new Avalonia.Thickness(0),
                    Background = Brushes.White,
                    Foreground = new SolidColorBrush(Color.Parse("#2196F3")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
                    BorderThickness = new Avalonia.Thickness(1),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                
                var container = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 1
                };
                container.Children.Add(valueBtn);
                container.Children.Add(dropBtn);
                
                // Shared click handler for both buttons
                EventHandler<Avalonia.Interactivity.RoutedEventArgs> showDropdown = (s, e) =>
                {
                    var combo = new ComboBox
                    {
                        Width = 78,
                        Height = 18,
                        FontSize = 11,
                        BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
                        BorderThickness = new Avalonia.Thickness(2)
                    };
                    
                    foreach (var val in Enum.GetValues(propType))
                        combo.Items.Add(val);
                    combo.SelectedItem = currentValue;
                    
                    combo.SelectionChanged += (s2, e2) =>
                    {
                        if (combo.SelectedItem != null)
                        {
                            // Update button
                            valueBtn.Content = combo.SelectedItem.ToString();
                            
                            // Update property
                            SetProperty(target, prop, combo.SelectedItem);
                            
                            // Swap back to buttons
                            var parent = combo.Parent as Panel;
                            if (parent != null)
                            {
                                var idx = parent.Children.IndexOf(combo);
                                parent.Children.RemoveAt(idx);
                                parent.Children.Insert(idx, container);
                            }
                        }
                    };
                    
                    combo.DropDownClosed += (s2, e2) =>
                    {
                        // Swap back to buttons if dropdown closed without selection
                        var parent = combo.Parent as Panel;
                        if (parent != null && parent.Children.Contains(combo))
                        {
                            var idx = parent.Children.IndexOf(combo);
                            parent.Children.RemoveAt(idx);
                            parent.Children.Insert(idx, container);
                        }
                    };
                    
                    // Swap container for ComboBox
                    var parent = container.Parent as Panel;
                    if (parent != null)
                    {
                        var idx = parent.Children.IndexOf(container);
                        parent.Children.RemoveAt(idx);
                        parent.Children.Insert(idx, combo);
                        combo.Focus();
                        
                        // Open dropdown immediately
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => combo.IsDropDownOpen = true, Avalonia.Threading.DispatcherPriority.Background);
                    }
                };
                
                // Wire both buttons to same handler
                valueBtn.Click += showDropdown;
                dropBtn.Click += showDropdown;
                
                editor = container;
            }
            else if (propType == typeof(Avalonia.Thickness) || propType == typeof(Avalonia.Thickness?))
            {
                var thickness = (Avalonia.Thickness?)currentValue ?? new Avalonia.Thickness(0);
                var thickBox = new TextBox 
                { 
                    Text = $"{thickness.Left},{thickness.Top},{thickness.Right},{thickness.Bottom}",
                    Width = 80,
                    FontSize = 11,
                    Padding = new Avalonia.Thickness(3, 2, 3, 2),
                    BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
                    BorderThickness = new Avalonia.Thickness(1),
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
                    Width = 70,
                    FontSize = 11,
                    Padding = new Avalonia.Thickness(3, 2, 3, 2),
                    BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
                    BorderThickness = new Avalonia.Thickness(1),
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
                    MinWidth = 80,
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
                    Padding = new Avalonia.Thickness(3, 1, 3, 0),
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






