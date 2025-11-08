using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace VB;

public static class VmlUiBuilder
{
    public static Control? BuildFromVml(Dictionary<string, string> props)
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) }); // Left panel
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Editor
        
        // LEFT PANEL - Controls and combos
        var leftPanel = new DockPanel();
        Grid.SetColumn(leftPanel, 0);
        
        var leftBorder = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2d2d2d")),
            BorderBrush = new SolidColorBrush(Color.Parse("#444")),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(15),
            Child = leftPanel
        };
        Grid.SetColumn(leftBorder, 0);
        root.Children.Add(leftBorder);
        
        var leftStack = new StackPanel { Spacing = 15 };
        leftPanel.Children.Add(leftStack);
        
        // Event combo
        leftStack.Children.Add(new TextBlock
        {
            Text = "When does this run?",
            Foreground = new SolidColorBrush(Color.Parse("#d4d4d4")),
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        });
        
        var eventCombo = new ComboBox 
        { 
            SelectedIndex = 0, 
            Name = "eventCombo",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        eventCombo.Items.Add("onClick (when clicked)");
        eventCombo.Items.Add("onLoad (when form loads)");
        eventCombo.Items.Add("onFocus (gets focus)");
        eventCombo.Items.Add("onBlur (loses focus)");
        eventCombo.Items.Add("onKeyPress (key pressed)");
        eventCombo.Items.Add("onMouseEnter (hover start)");
        eventCombo.Items.Add("onMouseLeave (hover end)");
        leftStack.Children.Add(eventCombo);
        
        // Interpreter combo
        leftStack.Children.Add(new TextBlock
        {
            Text = "What language?",
            Foreground = new SolidColorBrush(Color.Parse("#d4d4d4")),
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 10, 0, 5)
        });
        
        var interpreterCombo = new ComboBox 
        { 
            SelectedIndex = 0, 
            Name = "interpreterCombo",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        interpreterCombo.Items.Add("bash");
        interpreterCombo.Items.Add("python");
        interpreterCombo.Items.Add("node (JavaScript)");
        interpreterCombo.Items.Add("ruby");
        interpreterCombo.Items.Add("C# (coming soon)");
        interpreterCombo.Items.Add("perl");
        leftStack.Children.Add(interpreterCombo);
        
        // Instructions
        leftStack.Children.Add(new TextBlock
        {
            Text = "Access controls by name:\n• Button_1, Button_2\n• TextBox_1, TextBox_2\n\nGet property:\n  $(vb-get TextBox_1.Text)\n\nSet property:\n  vb-set Button_1.Content \"Hi!\"\n\nEnv vars:\n• $VML_CONTROLNAME\n• $VML_EVENTTYPE\n\nDB: /tmp/vb-runtime.db",
            Foreground = new SolidColorBrush(Color.Parse("#999")),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 20, 0, 0)
        });
        
        // RIGHT PANEL - Editor + buttons
        var rightPanel = new DockPanel();
        Grid.SetColumn(rightPanel, 1);
        root.Children.Add(rightPanel);
        
        // Button bar
        var buttonBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
            BorderBrush = new SolidColorBrush(Color.Parse("#444")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(10, 8)
        };
        
        var buttonStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };
        
        buttonStack.Children.Add(CreateButton("💾 Save & Close", "#0e7490"));
        buttonStack.Children.Add(CreateButton("❌ Cancel", "#555"));
        buttonStack.Children.Add(new Border { Width = 20 });
        buttonStack.Children.Add(CreateButton("▶ Test Run", "#16a34a"));
        buttonStack.Children.Add(CreateButton("📋 Clear", "#555"));
        
        buttonBar.Child = buttonStack;
        DockPanel.SetDock(buttonBar, Dock.Top);
        rightPanel.Children.Add(buttonBar);
        
        // Editor - BLACK with READABLE GREEN
        var scriptEditor = new TextBox
        {
            Name = "scriptEditor",
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Courier New, monospace"),
            FontSize = 13,
            Padding = new Thickness(10),
            Background = Brushes.Black,
            Foreground = new SolidColorBrush(Color.Parse("#00cc00")),
            CaretBrush = new SolidColorBrush(Color.Parse("#00ff00")),
            Text = @"#!/bin/bash
# Script for this control
# Access other controls via SQLite database

# Get property value:
TEXT=$(vb-get TextBox_1.Text)
echo ""Current text: $TEXT""

# Set property value:
vb-set Button_1.Content ""Updated!""

# Environment variables:
echo ""Control: $VML_CONTROLNAME""
echo ""Event: $VML_EVENTTYPE""

# Database location: /tmp/vb-runtime.db

# Your code here:
echo ""Script running!""
"
        };
        
        var editorScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = scriptEditor
        };
        
        rightPanel.Children.Add(editorScroll);
        
        Console.WriteLine("[VML UI] Built Script Editor - L->R workflow!");
        return root;
    }
    
    private static Button CreateButton(string text, string color)
    {
        return new Button
        {
            Content = text,
            Padding = new Thickness(12, 6),
            Background = new SolidColorBrush(Color.Parse(color)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
    }
}

