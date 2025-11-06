using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;

namespace VB;

public class OptionsWindow : Window
{
    private ListBox categoryList;
    private Panel contentPanel;
    private Dictionary<string, Panel> categoryPanels;
    
    public OptionsWindow()
    {
        Title = "Options - Visualised";
        Width = 900;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        categoryPanels = new Dictionary<string, Panel>();
        
        var mainGrid = new Grid();
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
        // LEFT: Category list
        var leftPanel = new DockPanel();
        
        var leftTitle = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2196F3")),
            Padding = new Avalonia.Thickness(15, 10),
            Child = new TextBlock 
            { 
                Text = "Categories",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            }
        };
        DockPanel.SetDock(leftTitle, Dock.Top);
        leftPanel.Children.Add(leftTitle);
        
        categoryList = new ListBox 
        { 
            Background = new SolidColorBrush(Color.Parse("#f5f5f5")),
            BorderThickness = new Avalonia.Thickness(0)
        };
        categoryList.Items.Add("General");
        categoryList.Items.Add("Designer");
        categoryList.Items.Add("ODBC");
        categoryList.Items.Add("Advanced");
        categoryList.Items.Add("Plugins (v2.0)");
        categoryList.Items.Add("Themes (v2.0)");
        categoryList.Items.Add("Git (v2.0)");
        categoryList.Items.Add("Collaboration (v2.0)");
        categoryList.SelectedIndex = 0;
        categoryList.SelectionChanged += Category_SelectionChanged;
        
        leftPanel.Children.Add(categoryList);
        
        Grid.SetColumn(leftPanel, 0);
        mainGrid.Children.Add(new Border 
        { 
            Child = leftPanel,
            BorderBrush = new SolidColorBrush(Color.Parse("#ccc")),
            BorderThickness = new Avalonia.Thickness(0, 0, 1, 0)
        });
        
        // RIGHT: Content area
        var rightPanel = new DockPanel();
        
        contentPanel = new StackPanel { Margin = new Avalonia.Thickness(20) };
        rightPanel.Children.Add(new ScrollViewer { Content = contentPanel });
        
        // Bottom buttons
        var buttonPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Avalonia.Thickness(20, 10),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        
        var okBtn = new Button 
        { 
            Content = "OK",
            Padding = new Avalonia.Thickness(25, 8),
            Background = new SolidColorBrush(Color.Parse("#66bb6a")),
            Foreground = Brushes.White
        };
        okBtn.Click += (s, e) => { SaveOptions(); Close(); };
        
        var cancelBtn = new Button 
        { 
            Content = "Cancel",
            Padding = new Avalonia.Thickness(25, 8)
        };
        cancelBtn.Click += (s, e) => Close();
        
        var applyBtn = new Button 
        { 
            Content = "Apply",
            Padding = new Avalonia.Thickness(25, 8)
        };
        applyBtn.Click += (s, e) => SaveOptions();
        
        buttonPanel.Children.Add(okBtn);
        buttonPanel.Children.Add(cancelBtn);
        buttonPanel.Children.Add(applyBtn);
        
        DockPanel.SetDock(buttonPanel, Dock.Bottom);
        rightPanel.Children.Add(new Border
        {
            Child = buttonPanel,
            BorderBrush = new SolidColorBrush(Color.Parse("#ccc")),
            BorderThickness = new Avalonia.Thickness(0, 1, 0, 0),
            Background = new SolidColorBrush(Color.Parse("#fafafa"))
        });
        
        Grid.SetColumn(rightPanel, 1);
        mainGrid.Children.Add(rightPanel);
        
        Content = mainGrid;
        
        BuildCategoryPanels();
        ShowCategory("General");
    }
    
    private void BuildCategoryPanels()
    {
        categoryPanels["General"] = BuildGeneralPanel();
        categoryPanels["Designer"] = BuildDesignerPanel();
        categoryPanels["ODBC"] = BuildODBCPanel();
        categoryPanels["Advanced"] = BuildAdvancedPanel();
        categoryPanels["Plugins (v2.0)"] = BuildPlaceholderPanel("Plugin System", "Plugin management and custom extensions");
        categoryPanels["Themes (v2.0)"] = BuildPlaceholderPanel("Themes", "Custom color schemes and UI themes");
        categoryPanels["Git (v2.0)"] = BuildPlaceholderPanel("Git Integration", "Source control and versioning");
        categoryPanels["Collaboration (v2.0)"] = BuildPlaceholderPanel("Collaboration", "Team features and real-time editing");
    }
    
    private Panel BuildGeneralPanel()
    {
        var panel = new StackPanel { Spacing = 15 };
        
        panel.Children.Add(new TextBlock { Text = "General Settings", FontSize = 18, FontWeight = FontWeight.Bold });
        
        panel.Children.Add(new TextBlock { Text = "Auto-save:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) });
        var autoSaveCheck = new CheckBox { Content = "Enable auto-save", IsChecked = true };
        panel.Children.Add(autoSaveCheck);
        
        panel.Children.Add(new TextBlock { Text = "Auto-save interval (minutes):", Margin = new Avalonia.Thickness(20, 5, 0, 0) });
        var intervalBox = new NumericUpDown { Value = 5, Minimum = 1, Maximum = 60, Width = 100 };
        panel.Children.Add(new StackPanel { Margin = new Avalonia.Thickness(20, 0, 0, 0), Children = { intervalBox } });
        
        panel.Children.Add(new TextBlock { Text = "Recent Projects:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 20, 0, 0) });
        panel.Children.Add(new TextBlock { Text = "Number to remember:" });
        var recentBox = new NumericUpDown { Value = 10, Minimum = 1, Maximum = 50, Width = 100 };
        panel.Children.Add(new StackPanel { Margin = new Avalonia.Thickness(20, 0, 0, 0), Children = { recentBox } });
        
        panel.Children.Add(new TextBlock { Text = "Startup:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 20, 0, 0) });
        var startupCheck = new CheckBox { Content = "Open last project on startup", IsChecked = false };
        panel.Children.Add(startupCheck);
        
        return panel;
    }
    
    private Panel BuildDesignerPanel()
    {
        var panel = new StackPanel { Spacing = 15 };
        
        panel.Children.Add(new TextBlock { Text = "Designer Settings", FontSize = 18, FontWeight = FontWeight.Bold });
        
        panel.Children.Add(new TextBlock { Text = "Grid:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) });
        var showGridCheck = new CheckBox { Content = "Show grid", IsChecked = true };
        panel.Children.Add(showGridCheck);
        
        var snapGridCheck = new CheckBox { Content = "Snap to grid", IsChecked = false };
        panel.Children.Add(snapGridCheck);
        
        panel.Children.Add(new TextBlock { Text = "Grid size:", Margin = new Avalonia.Thickness(20, 5, 0, 0) });
        var gridSizeBox = new NumericUpDown { Value = 10, Minimum = 1, Maximum = 100, Width = 100 };
        panel.Children.Add(new StackPanel { Margin = new Avalonia.Thickness(20, 0, 0, 0), Children = { gridSizeBox } });
        
        panel.Children.Add(new TextBlock { Text = "Guides:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 20, 0, 0) });
        var showGuidesCheck = new CheckBox { Content = "Show guide rectangle", IsChecked = true };
        panel.Children.Add(showGuidesCheck);
        
        panel.Children.Add(new TextBlock { Text = "Guide dimensions:", Margin = new Avalonia.Thickness(20, 5, 0, 0) });
        var guideDims = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Avalonia.Thickness(20, 0, 0, 0) };
        guideDims.Children.Add(new TextBlock { Text = "Width:", VerticalAlignment = VerticalAlignment.Center });
        guideDims.Children.Add(new NumericUpDown { Value = 800, Minimum = 100, Maximum = 5000, Width = 100 });
        guideDims.Children.Add(new TextBlock { Text = "Height:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(10, 0, 0, 0) });
        guideDims.Children.Add(new NumericUpDown { Value = 600, Minimum = 100, Maximum = 5000, Width = 100 });
        panel.Children.Add(guideDims);
        
        panel.Children.Add(new TextBlock { Text = "Behavior:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 20, 0, 0) });
        var ghostCheck = new CheckBox { Content = "Show ghost on drag", IsChecked = true };
        panel.Children.Add(ghostCheck);
        
        return panel;
    }
    
    private Panel BuildODBCPanel()
    {
        var panel = new StackPanel { Spacing = 15 };
        
        panel.Children.Add(new TextBlock { Text = "ODBC Settings", FontSize = 18, FontWeight = FontWeight.Bold });
        
        panel.Children.Add(new TextBlock { Text = "Database Type:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) });
        var dbTypeCombo = new ComboBox { Width = 300, SelectedIndex = 0 };
        dbTypeCombo.Items.Add("SQLite");
        dbTypeCombo.Items.Add("MySQL");
        dbTypeCombo.Items.Add("PostgreSQL");
        dbTypeCombo.Items.Add("SQL Server");
        dbTypeCombo.Items.Add("Oracle");
        dbTypeCombo.Items.Add("Custom ODBC");
        panel.Children.Add(dbTypeCombo);
        
        panel.Children.Add(new TextBlock { Text = "Connection String:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 20, 0, 0) });
        var connStrBox = new TextBox 
        { 
            Text = "Data Source=mydb.db;Version=3;",
            Width = 600,
            Height = 60,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        panel.Children.Add(connStrBox);
        
        var testBtn = new Button 
        { 
            Content = "Test Connection",
            Padding = new Avalonia.Thickness(20, 8),
            Margin = new Avalonia.Thickness(0, 10, 0, 0)
        };
        testBtn.Click += (s, e) => 
        {
            // TODO: Test connection
            var msg = new Window { Title = "Test Connection", Width = 300, Height = 150 };
            msg.Content = new StackPanel 
            { 
                Margin = new Avalonia.Thickness(20),
                Spacing = 15,
                Children = 
                {
                    new TextBlock { Text = "Connection successful!", FontSize = 14 },
                    new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Center }
                }
            };
            msg.ShowDialog((Window)this.Parent!);
        };
        panel.Children.Add(testBtn);
        
        panel.Children.Add(new TextBlock { Text = "Server:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 20, 0, 0) });
        panel.Children.Add(new TextBox { Watermark = "localhost", Width = 300 });
        
        panel.Children.Add(new TextBlock { Text = "Port:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) });
        panel.Children.Add(new TextBox { Watermark = "3306", Width = 100 });
        
        panel.Children.Add(new TextBlock { Text = "Database:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) });
        panel.Children.Add(new TextBox { Watermark = "mydatabase", Width = 300 });
        
        panel.Children.Add(new TextBlock { Text = "Username:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) });
        panel.Children.Add(new TextBox { Watermark = "user", Width = 300 });
        
        panel.Children.Add(new TextBlock { Text = "Password:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) });
        panel.Children.Add(new TextBox { Watermark = "password", PasswordChar = '*', Width = 300 });
        
        return panel;
    }
    
    private Panel BuildAdvancedPanel()
    {
        var panel = new StackPanel { Spacing = 15 };
        
        panel.Children.Add(new TextBlock { Text = "Advanced Settings", FontSize = 18, FontWeight = FontWeight.Bold });
        
        panel.Children.Add(new TextBlock { Text = "VML Engine:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) });
        var strictCheck = new CheckBox { Content = "Strict parsing mode", IsChecked = false };
        panel.Children.Add(strictCheck);
        
        var cacheCheck = new CheckBox { Content = "Cache compiled VML", IsChecked = true };
        panel.Children.Add(cacheCheck);
        
        panel.Children.Add(new TextBlock { Text = "Performance:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 20, 0, 0) });
        var hwAccelCheck = new CheckBox { Content = "Hardware acceleration", IsChecked = true };
        panel.Children.Add(hwAccelCheck);
        
        panel.Children.Add(new TextBlock { Text = "Logging:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 20, 0, 0) });
        panel.Children.Add(new TextBlock { Text = "Log level:" });
        var logLevelCombo = new ComboBox { Width = 200, SelectedIndex = 1 };
        logLevelCombo.Items.Add("None");
        logLevelCombo.Items.Add("Error");
        logLevelCombo.Items.Add("Warning");
        logLevelCombo.Items.Add("Info");
        logLevelCombo.Items.Add("Debug");
        panel.Children.Add(logLevelCombo);
        
        panel.Children.Add(new TextBlock { Text = "Experimental:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 20, 0, 0) });
        var expCheck = new CheckBox { Content = "Enable experimental features", IsChecked = false };
        panel.Children.Add(expCheck);
        
        return panel;
    }
    
    private Panel BuildPlaceholderPanel(string title, string description)
    {
        var panel = new StackPanel { Spacing = 15 };
        
        panel.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeight.Bold });
        panel.Children.Add(new TextBlock 
        { 
            Text = description,
            FontStyle = FontStyle.Italic,
            Foreground = new SolidColorBrush(Color.Parse("#666"))
        });
        
        panel.Children.Add(new Border
        {
            Margin = new Avalonia.Thickness(0, 20, 0, 0),
            Padding = new Avalonia.Thickness(20),
            Background = new SolidColorBrush(Color.Parse("#f0f0f0")),
            BorderBrush = new SolidColorBrush(Color.Parse("#ccc")),
            BorderThickness = new Avalonia.Thickness(1),
            Child = new TextBlock
            {
                Text = "Coming in Version 2.0\n\nThis feature is planned for the next major release.\nStay tuned for updates!",
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#999"))
            }
        });
        
        return panel;
    }
    
    private void Category_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (categoryList.SelectedItem != null)
        {
            ShowCategory(categoryList.SelectedItem.ToString()!);
        }
    }
    
    private void ShowCategory(string category)
    {
        contentPanel.Children.Clear();
        if (categoryPanels.ContainsKey(category))
        {
            contentPanel.Children.Add(categoryPanels[category]);
        }
    }
    
    private void SaveOptions()
    {
        Console.WriteLine("[OPTIONS] Saved");
        // TODO: Save options to config file
    }
}



