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
    var picker = new ColorPicker
    {
        Color = currentColor,
        Width = 250,
        Height = 300
    };
    
    var window = new Window
    {
        Title = "Pick Color",
        Width = 270,
        Height = 340,
        Content = new StackPanel
        {
            Children =
            {
                picker,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(5),
                    Spacing = 5,
                    Children =
                    {
                        new Button { Content = "OK", Width = 60 },
                        new Button { Content = "Cancel", Width = 60 }
                    }
                }
            }
        },
        CanResize = false
    };
    
    var okBtn = ((window.Content as StackPanel)!.Children[1] as StackPanel)!.Children[0] as Button;
    var cancelBtn = ((window.Content as StackPanel)!.Children[1] as StackPanel)!.Children[1] as Button;
    
    okBtn!.Click += (s, e) =>
    {
        currentColor = picker.Color;
        hexLabel.Content = currentColor.ToString();
        ColorChanged?.Invoke(this, currentColor);
        window.Close();
    };
    
    cancelBtn!.Click += (s, e) => window.Close();
    
    await window.ShowDialog((Window)this.VisualRoot!);
}

}
