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
    
private async void ShowColorPicker()
{
    var colorView = new ColorView
    {
        Color = currentColor,
        Width = 350,
        Height = 400
    };
    
    var okBtn = new Button { Content = "OK", Width = 80, Margin = new Thickness(5) };
    var cancelBtn = new Button { Content = "Cancel", Width = 80, Margin = new Thickness(5) };
    
    var buttonPanel = new StackPanel
    {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Children = { okBtn, cancelBtn }
    };
    
    var content = new StackPanel
    {
        Children = { colorView, buttonPanel }
    };
    
    var window = new Window
    {
        Title = "Pick Color",
        Content = content,
        Width = 320,
        Height = 420,
        CanResize = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner
    };
    
    okBtn.Click += (s, e) =>
    {
        currentColor = colorView.Color;
        hexLabel.Content = currentColor.ToString();
        ColorChanged?.Invoke(this, currentColor);
        window.Close();
    };
    
    cancelBtn.Click += (s, e) => window.Close();
    
    await window.ShowDialog((Window)this.VisualRoot!);
}
}
