using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;

namespace VB;

public class TinyCheckBox : CheckBox
{
    public TinyCheckBox()
    {
        MinWidth = 18;
        Height = 18;
        MinHeight = 18;
        FontSize = 11;
        Padding = new Thickness(0);
        Margin = new Thickness(0);  // Semicolon, not comma
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalContentAlignment = VerticalAlignment.Center;
        BorderBrush = new SolidColorBrush(Color.Parse("#66bb6a"));
        BorderThickness = new Thickness(1);
    }
}
