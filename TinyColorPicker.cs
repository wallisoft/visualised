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
            Height = 20,
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
    var picker = new ColorPicker
    {
        Color = currentColor
    };
    
    picker.ColorChanged += (s, e) =>
    {
        currentColor = e.NewColor;
        hexLabel.Content = currentColor.ToString();
        ColorChanged?.Invoke(this, currentColor);
    };
    
    var flyout = new Flyout
    {
        Content = picker,
        Placement = PlacementMode.Right
    };
    
    flyout.ShowAt(pickBtn);
}
}
