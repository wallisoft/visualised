using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;

namespace VB;

public class TinyColorPicker : StackPanel
{
    private Border colorSwatch;
    private Button pickBtn;
    private Panel? parentPanel;
    private Color currentColor = Colors.White;
    
    public Color Color
    {
        get => currentColor;
        set
        {
            currentColor = value;
            colorSwatch.Background = new SolidColorBrush(value);
        }
    }
    
    public event EventHandler<Color>? ColorChanged;
    
    public TinyColorPicker()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 0;
        
        colorSwatch = new Border
        {
            Width = 120,
            MinHeight = 15,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(1, 1, 0, 1),
            CornerRadius = new CornerRadius(2, 0, 0, 2),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        
        pickBtn = new Button
        {
            Content = "♻",
            Width = 18,
            Height = 18,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(0, -1, 0, 0),
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.Parse("#ff6600")),
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(0, 1, 1, 1),
            CornerRadius = new CornerRadius(0, 2, 2, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        
        pickBtn.Click += (s, e) => ShowColorPicker();
        colorSwatch.PointerPressed += (s, e) =>
        {
            ShowColorPicker();
            e.Handled = true;
        };
        
        Children.Add(colorSwatch);
        Children.Add(pickBtn);
    }
    
    private void ShowColorPicker()
    {
        parentPanel = this.Parent as Panel;
        if (parentPanel == null) return;
        
        // Simple hex input for now - can expand to full picker later
        var hexBox = new TextBox
        {
            Text = currentColor.ToString(),
            Width = 138,
            Height = 18,
            FontSize = 11,
            Padding = new Thickness(4, 0, 4, 0),
            BorderBrush = new SolidColorBrush(Color.Parse("#2196F3")),
            BorderThickness = new Thickness(1)
        };
        
        hexBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                TryParseColor(hexBox.Text);
                SwapBack(hexBox);
                e.Handled = true;
            }
        };
        
        hexBox.LostFocus += (s, e) =>
        {
            TryParseColor(hexBox.Text);
            SwapBack(hexBox);
        };
        
        var index = parentPanel.Children.IndexOf(this);
        parentPanel.Children.RemoveAt(index);
        parentPanel.Children.Insert(index, hexBox);
        hexBox.Focus();
        hexBox.SelectAll();
    }
    
    private void TryParseColor(string text)
    {
        try
        {
            currentColor = Color.Parse(text);
            colorSwatch.Background = new SolidColorBrush(currentColor);
            ColorChanged?.Invoke(this, currentColor);
        }
        catch
        {
            // Invalid color, keep current
        }
    }
    
    private void SwapBack(TextBox hexBox)
    {
        if (parentPanel == null) return;
        
        var idx = parentPanel.Children.IndexOf(hexBox);
        if (idx >= 0)
        {
            parentPanel.Children.RemoveAt(idx);
            parentPanel.Children.Insert(idx, this);
        }
    }
}
