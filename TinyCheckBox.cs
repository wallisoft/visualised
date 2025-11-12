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
        Height = 18;
        MinHeight = 18;
        FontSize = 11;
        Padding = new Thickness(0);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalContentAlignment = VerticalAlignment.Center;
    }
}
