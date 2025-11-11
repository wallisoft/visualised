using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VB;

public class TinyFontPicker : StackPanel
{
    private Label fontLabel;
    private Button pickBtn;
    private FontFamily currentFamily = FontFamily.Default;
    private double currentSize = 12;
    private FontWeight currentWeight = FontWeight.Normal;
    private FontStyle currentStyle = FontStyle.Normal;
    
    public FontFamily Family
    {
        get => currentFamily;
        set { currentFamily = value; UpdateLabel(); }
    }
    
    public double Size
    {
        get => currentSize;
        set { currentSize = value; UpdateLabel(); }
    }
    
    public FontWeight Weight
    {
        get => currentWeight;
        set { currentWeight = value; UpdateLabel(); }
    }
    
    public FontStyle Style
    {
        get => currentStyle;
        set { currentStyle = value; UpdateLabel(); }
    }
    
    public event EventHandler<(FontFamily family, double size, FontWeight weight, FontStyle style)>? FontChanged;
    
    public TinyFontPicker()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 0;
        
        fontLabel = new Label
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
        
        UpdateLabel();
        
        pickBtn.Click += (s, e) => ShowFontPicker();
        fontLabel.PointerPressed += (s, e) =>
        {
            ShowFontPicker();
            e.Handled = true;
        };
        
        Children.Add(fontLabel);
        Children.Add(pickBtn);
    }
    
    private void UpdateLabel()
    {
        var weightStr = currentWeight == FontWeight.Bold ? " Bold" : "";
        var styleStr = currentStyle == FontStyle.Italic ? " Italic" : "";
        fontLabel.Content = $"{currentFamily.Name} {currentSize}{weightStr}{styleStr}";
    }
    
    private async void ShowFontPicker()
    {
        // Common fonts
        var fonts = new[] 
        { 
            "Arial", "Calibri", "Cambria", "Comic Sans MS", "Consolas", "Courier New", 
            "Georgia", "Helvetica", "Lucida Console", "Segoe UI", "Times New Roman", 
            "Trebuchet MS", "Verdana"
        };
        
        var familyCombo = new ComboBox { Width = 200, SelectedItem = currentFamily.Name };
        foreach (var font in fonts)
            familyCombo.Items.Add(font);
        
        var sizeBox = new NumericUpDown { Width = 80, Value = (decimal)currentSize, Minimum = 6, Maximum = 72, Increment = 1 };
        
        var weightCombo = new ComboBox { Width = 120, SelectedItem = currentWeight.ToString() };
        weightCombo.Items.Add("Normal");
        weightCombo.Items.Add("Bold");
        
        var styleCombo = new ComboBox { Width = 120, SelectedItem = currentStyle.ToString() };
        styleCombo.Items.Add("Normal");
        styleCombo.Items.Add("Italic");
        
        var previewText = new TextBlock 
        { 
	    Text = "The quick brown fox jumps over the lazy dog",
	    FontFamily = currentFamily,
	    FontSize = currentSize,
	    FontWeight = currentWeight,
	    FontStyle = currentStyle,
	    TextWrapping = TextWrapping.Wrap,
	    Padding = new Thickness(10)
        };

	var preview = new Border
	{
	    Child = previewText,
	    Margin = new Thickness(0, 10, 0, 0),
	    Background = Brushes.White,
	    BorderBrush = Brushes.LightGray,
	    BorderThickness = new Thickness(1)
	};
        
        // Update preview on changes
	familyCombo.SelectionChanged += (s, e) =>
	{
	    if (familyCombo.SelectedItem != null)
		previewText.FontFamily = new FontFamily(familyCombo.SelectedItem.ToString()!);
	};

	sizeBox.ValueChanged += (s, e) => previewText.FontSize = (double)(sizeBox.Value ?? 12);

	weightCombo.SelectionChanged += (s, e) =>
	{
	    previewText.FontWeight = weightCombo.SelectedItem?.ToString() == "Bold" ? FontWeight.Bold : FontWeight.Normal;
	};

	styleCombo.SelectionChanged += (s, e) =>
	{
	    previewText.FontStyle = styleCombo.SelectedItem?.ToString() == "Italic" ? FontStyle.Italic : FontStyle.Normal;
};
        
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,*"),
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Margin = new Thickness(10)
        };
        
        grid.Children.Add(new TextBlock { Text = "Font Family:", Margin = new Thickness(0, 0, 10, 5) });
        Grid.SetRow(grid.Children[^1], 0);
        Grid.SetColumn(grid.Children[^1], 0);
        
        grid.Children.Add(familyCombo);
        Grid.SetRow(grid.Children[^1], 0);
        Grid.SetColumn(grid.Children[^1], 1);
        
        grid.Children.Add(new TextBlock { Text = "Size:", Margin = new Thickness(0, 5, 10, 5) });
        Grid.SetRow(grid.Children[^1], 1);
        Grid.SetColumn(grid.Children[^1], 0);
        
        grid.Children.Add(sizeBox);
        Grid.SetRow(grid.Children[^1], 1);
        Grid.SetColumn(grid.Children[^1], 1);
        
        grid.Children.Add(new TextBlock { Text = "Weight:", Margin = new Thickness(0, 5, 10, 5) });
        Grid.SetRow(grid.Children[^1], 2);
        Grid.SetColumn(grid.Children[^1], 0);
        
        grid.Children.Add(weightCombo);
        Grid.SetRow(grid.Children[^1], 2);
        Grid.SetColumn(grid.Children[^1], 1);
        
        grid.Children.Add(new TextBlock { Text = "Style:", Margin = new Thickness(0, 5, 10, 5) });
        Grid.SetRow(grid.Children[^1], 3);
        Grid.SetColumn(grid.Children[^1], 0);
        
        grid.Children.Add(styleCombo);
        Grid.SetRow(grid.Children[^1], 3);
        Grid.SetColumn(grid.Children[^1], 1);
        
        grid.Children.Add(preview);
        Grid.SetRow(grid.Children[^1], 4);
        Grid.SetColumn(grid.Children[^1], 1);
        
        var okBtn = new Button 
        { 
            Content = "OK", 
            Width = 80,
            FontWeight = FontWeight.Bold,
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.Parse("#2e7d32")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2e7d32")),
            BorderThickness = new Thickness(2),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        var cancelBtn = new Button 
        { 
            Content = "Cancel", 
            Width = 80,
            FontWeight = FontWeight.Bold,
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.Parse("#2e7d32")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2e7d32")),
            BorderThickness = new Thickness(2),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0, 10, 0, 0),
            Children = { okBtn, cancelBtn }
        };
        
        var container = new Border
        {
            Padding = new Thickness(20),
            Child = new StackPanel
            {
                Children = { grid, buttonPanel }
            }
        };
        
        var window = new Window
        {
            Title = "Pick Font",
            Content = container,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        
        okBtn.Click += (s, e) =>
        {
            currentFamily = new FontFamily(familyCombo.SelectedItem?.ToString() ?? "Arial");
            currentSize = (double)(sizeBox.Value ?? 12);
            currentWeight = weightCombo.SelectedItem?.ToString() == "Bold" ? FontWeight.Bold : FontWeight.Normal;
            currentStyle = styleCombo.SelectedItem?.ToString() == "Italic" ? FontStyle.Italic : FontStyle.Normal;
            
            UpdateLabel();
            FontChanged?.Invoke(this, (currentFamily, currentSize, currentWeight, currentStyle));
            window.Close();
        };
        
        cancelBtn.Click += (s, e) => window.Close();
        
        await window.ShowDialog((Window)this.VisualRoot!);
    }
}
