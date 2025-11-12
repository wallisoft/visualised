using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;

namespace VB;

public class TinyCheckBox : Border
{
    private TextBlock indicator;
    private bool isChecked;
    
    public bool? IsChecked
    {
        get => isChecked;
        set
        {
            isChecked = value ?? false;
            indicator.Text = isChecked ? "✕" : "";
        }
    }
    
    public TinyCheckBox()
    {
        Width = 15;
        Height = 15;
        Background = Brushes.White;
        BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a"));
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(2);
        HorizontalAlignment = HorizontalAlignment.Left;
        IsHitTestVisible = false;  // No clicking
        
        indicator = new TextBlock
        {
            Text = "",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#2196F3")),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, -2, 0, 0)  // Nudge up slightly
        };
        
        Child = indicator;
    }
}
