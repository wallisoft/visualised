using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;

namespace VB;

public class TinyColorPicker : StackPanel
{
    private Label hexLabel;
    private Button pickBtn;
    private Panel? parentPanel;
    private Color currentColor = Colors.White;
    
    public Color Color
    {
        get => currentColor;
        set
        {
            currentColor = value;
            hexLabel.Content = currentColor.ToString();
        }
    }
    
    public event EventHandler<Color>? ColorChanged;
    
    public TinyColorPicker()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 0;
        
        hexLabel = new Label
        {
            Width = 120,
            MinHeight = 15,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(4, 2, 4, 2),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(1, 1, 0, 1),
            CornerRadius = new CornerRadius(2, 0, 0, 2),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Center,
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
        hexLabel.PointerPressed += (s, e) =>
        {
            ShowColorPicker();
            e.Handled = true;
        };
        
        Children.Add(hexLabel);
        Children.Add(pickBtn);
    }
    
    private void ShowColorPicker()
    {
        parentPanel = this.Parent as Panel;
        if (parentPanel == null) return;
        
        var picker = new ColorPicker
        {
            Color = currentColor,
            Width = 200,
            Height = 150
        };
        
        picker.ColorChanged += (s, e) =>
        {
            currentColor = e.NewColor;
            hexLabel.Content = currentColor.ToString();
            ColorChanged?.Invoke(this, currentColor);
        };
        
        picker.LostFocus += (s, e) => SwapBack(picker);
        picker.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                SwapBack(picker);
                e.Handled = true;
            }
        };
        
        var index = parentPanel.Children.IndexOf(this);
        parentPanel.Children.RemoveAt(index);
        parentPanel.Children.Insert(index, picker);
        picker.Focus();
    }
    
    private void SwapBack(Control control)
    {
        if (parentPanel == null) return;
        
        var idx = parentPanel.Children.IndexOf(control);
        if (idx >= 0)
        {
            parentPanel.Children.RemoveAt(idx);
            parentPanel.Children.Insert(idx, this);
        }
    }
}
