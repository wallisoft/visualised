using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;

namespace VB;

public class TinyColorPicker : StackPanel
{
    private Label colorBox;
    private Button pickBtn;
    private Panel? parentPanel;
    private Color currentColor;
    
    public Color SelectedColor
    {
        get => currentColor;
        set
        {
            currentColor = value;
            colorBox.Background = new SolidColorBrush(value);
        }
    }
    
    public event EventHandler<Color>? ColorChanged;
    
    public TinyColorPicker()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 1;
        
        colorBox = new Label
        {
            Width = 120,
            MinHeight = 15,
            Padding = new Thickness(4, 2, 4, 2),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        
        pickBtn = new Button
        {
            Content = "♻",
            Width = 18,
            Height = 18,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.Parse("#ff6600")),  // ORANGE!
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        
        pickBtn.Click += (s, e) => ShowColorPicker();
        colorBox.PointerPressed += (s, e) =>
        {
            ShowColorPicker();
            e.Handled = true;
        };
        
        Children.Add(colorBox);
        Children.Add(pickBtn);
    }
    
    private async void ShowColorPicker()
    {
        // Simple color picker with common colors
        parentPanel = this.Parent as Panel;
        if (parentPanel == null) return;
        
        var colorPanel = new StackPanel { Background = Brushes.White, Margin = new Thickness(2) };
        
        var commonColors = new[]
        {
            ("White", "#FFFFFF"), ("Black", "#000000"),
            ("Red", "#FF0000"), ("Green", "#00FF00"), ("Blue", "#0000FF"),
            ("Yellow", "#FFFF00"), ("Orange", "#FF6600"), ("Purple", "#800080"),
            ("Gray", "#808080"), ("Pink", "#FFC0CB"),
            ("Teal", "#66bb6a"), ("Brown", "#8B4513")
        };
        
        var grid = new WrapPanel { Width = 260 };
        foreach (var (name, hex) in commonColors)
        {
            var btn = new Button
            {
                Content = name,
                Background = new SolidColorBrush(Color.Parse(hex)),
                Width = 60,
                Height = 25,
                Margin = new Thickness(2)
            };
            btn.Click += (s, e) =>
            {
                var color = Color.Parse(hex);
                SelectedColor = color;
                ColorChanged?.Invoke(this, color);
                CloseColorPicker(colorPanel);
            };
            grid.Children.Add(btn);
        }
        
        colorPanel.Children.Add(grid);
        
        var border = new Border
        {
            Child = colorPanel,
            BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a")),
            BorderThickness = new Thickness(2),
            Background = Brushes.White
        };
        
        var index = parentPanel.Children.IndexOf(this);
        parentPanel.Children.Insert(index + 1, border);
    }
    
    private void CloseColorPicker(Control colorPanel)
    {
        if (parentPanel == null) return;
        var border = colorPanel.Parent as Border;
        if (border != null)
            parentPanel.Children.Remove(border);
    }
}
