using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using System;
using System.Linq;

namespace VB;

public class MenuEditorWindow : Window
{
    private MainWindow parentWindow;
    private ComboBox menuTemplateCombo;
    private ComboBox contextTemplateCombo;
    private TreeView menuTree;
    private TreeView contextTree;
    private TextBox headerBox;
    private CheckBox visibleCheck;
    private CheckBox pinRightCheck;
    private ComboBox colorCombo;
    private Button moveUpBtn;
    private Button moveDownBtn;
    private Button addSepBtn;
    private Button deleteBtn;
    private TextBlock statusText;
    private bool isMainMenu = true;
    
    public MenuEditorWindow(MainWindow parent)
    {
        parentWindow = parent;
        
        Title = "📋 Menu Editor - Visualised";
        Width = 1200;
        Height = 750;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });
        
        // TOP: Template selection
        var topPanel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#f5f5f5")),
            BorderBrush = new SolidColorBrush(Color.Parse("#ccc")),
            BorderThickness = new Avalonia.Thickness(0, 0, 0, 1),
            Padding = new Avalonia.Thickness(15, 10)
        };
        
        var topStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20 };
        
        topStack.Children.Add(new TextBlock { Text = "Menu Template:", FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center });
        menuTemplateCombo = new ComboBox { Width = 200, SelectedIndex = 1 };
        menuTemplateCombo.Items.Add("Basic");
        menuTemplateCombo.Items.Add("VisualisedFormEditor");
        menuTemplateCombo.Items.Add("SimpleTextEditor");
        menuTemplateCombo.SelectionChanged += MenuTemplate_Changed;
        topStack.Children.Add(menuTemplateCombo);
        
        topStack.Children.Add(new Border { Width = 2, Height = 30, Background = new SolidColorBrush(Color.Parse("#ccc")) });
        
        topStack.Children.Add(new TextBlock { Text = "Context Menu:", FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center });
        contextTemplateCombo = new ComboBox { Width = 200, SelectedIndex = 1 };
        contextTemplateCombo.Items.Add("Basic");
        contextTemplateCombo.Items.Add("VisualisedEditor");
        contextTemplateCombo.Items.Add("TextEditor");
        contextTemplateCombo.SelectionChanged += ContextTemplate_Changed;
        topStack.Children.Add(contextTemplateCombo);
        
        topPanel.Child = topStack;
        Grid.SetRow(topPanel, 0);
        mainGrid.Children.Add(topPanel);
        
        // MIDDLE: Three columns
        var middleGrid = new Grid();
        middleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(350) });
        middleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        middleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        
        // Create trees and properties
        menuTree = new TreeView();
        contextTree = new TreeView();
        headerBox = new TextBox();
        visibleCheck = new CheckBox();
        pinRightCheck = new CheckBox();
        colorCombo = new ComboBox();
        moveUpBtn = new Button();
        moveDownBtn = new Button();
        addSepBtn = new Button();
        deleteBtn = new Button();
        statusText = new TextBlock();
        
        // LEFT: Main Menu Tree
        var leftPanel = CreateMenuTreePanel("Main Menu Structure", menuTree, true);
        Grid.SetColumn(leftPanel, 0);
        middleGrid.Children.Add(leftPanel);
        
        // MIDDLE: Properties Panel
        var propsPanel = CreatePropertiesPanel();
        Grid.SetColumn(propsPanel, 1);
        middleGrid.Children.Add(propsPanel);
        
        // RIGHT: Context Menu Tree
        var rightPanel = CreateMenuTreePanel("Context Menu Structure", contextTree, false);
        Grid.SetColumn(rightPanel, 2);
        middleGrid.Children.Add(rightPanel);
        
        Grid.SetRow(middleGrid, 1);
        mainGrid.Children.Add(middleGrid);
        
        // BOTTOM: Action buttons + Status
        var bottomPanel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#f5f5f5")),
            BorderBrush = new SolidColorBrush(Color.Parse("#ccc")),
            BorderThickness = new Avalonia.Thickness(0, 1, 0, 0),
            Padding = new Avalonia.Thickness(15, 10)
        };
        
        var bottomStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        
        var applyBtn = new Button 
        { 
            Content = "✓ Apply Templates", 
            Padding = new Avalonia.Thickness(25,10), 
            Background = new SolidColorBrush(Color.Parse("#66bb6a")), 
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeight.Bold
        };
        applyBtn.Click += ApplyTemplates;
        
        var saveBtn = new Button 
        { 
            Content = "💾 Save to VML", 
            Padding = new Avalonia.Thickness(25,10),
            Background = new SolidColorBrush(Color.Parse("#2196F3")),
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeight.Bold
        };
        saveBtn.Click += SaveToVML;
        
        var closeBtn = new Button 
        { 
            Content = "✕ Close", 
            Padding = new Avalonia.Thickness(25,10),
            FontSize = 14
        };
        closeBtn.Click += (s,e) => Close();
        
        bottomStack.Children.Add(applyBtn);
        bottomStack.Children.Add(saveBtn);
        bottomStack.Children.Add(closeBtn);
        
        statusText.Text = "Select menu items to edit properties";
        statusText.FontStyle = FontStyle.Italic;
        statusText.Foreground = new SolidColorBrush(Color.Parse("#666"));
        statusText.VerticalAlignment = VerticalAlignment.Center;
        statusText.Margin = new Avalonia.Thickness(20, 0, 0, 0);
        bottomStack.Children.Add(statusText);
        
        bottomPanel.Child = bottomStack;
        Grid.SetRow(bottomPanel, 2);
        mainGrid.Children.Add(bottomPanel);
        
        Content = mainGrid;
        
        LoadCurrentMenus();
    }
    
    private Border CreateMenuTreePanel(string title, TreeView tree, bool isMain)
    {
        var panel = new DockPanel();
        
        var titleBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#66bb6a")),
            Padding = new Avalonia.Thickness(10)
        };
        titleBar.Child = new TextBlock 
        { 
            Text = title,
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        };
        DockPanel.SetDock(titleBar, Dock.Top);
        panel.Children.Add(titleBar);
        
        tree.Margin = new Avalonia.Thickness(10);
        tree.SelectionChanged += (s, e) => 
        {
            isMainMenu = isMain;
            TreeSelectionChanged(tree);
        };
        panel.Children.Add(tree);
        
        return new Border 
        { 
            Child = panel,
            BorderBrush = new SolidColorBrush(Color.Parse("#ccc")),
            BorderThickness = new Avalonia.Thickness(0, 0, 1, 0)
        };
    }
    
    private Border CreatePropertiesPanel()
    {
        var panel = new StackPanel { Margin = new Avalonia.Thickness(15), Spacing = 12 };
        
        var titleBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2196F3")),
            Padding = new Avalonia.Thickness(10),
            Margin = new Avalonia.Thickness(-15, -15, -15, 15)
        };
        titleBar.Child = new TextBlock 
        { 
            Text = "Item Properties",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        };
        panel.Children.Add(titleBar);
        
        // Header
        panel.Children.Add(new TextBlock { Text = "Header Text:", FontWeight = FontWeight.Bold });
        headerBox.Watermark = "Menu text";
        panel.Children.Add(headerBox);
        
        // Visibility
        visibleCheck.Content = "Visible";
        visibleCheck.IsChecked = true;
        panel.Children.Add(visibleCheck);
        
        // Pin Right
        pinRightCheck.Content = "Pin to Right (Help, etc.)";
        pinRightCheck.IsChecked = false;
        panel.Children.Add(pinRightCheck);
        
        // Color
        panel.Children.Add(new TextBlock { Text = "Menu Color:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) });
        colorCombo.Width = 250;
        colorCombo.Items.Add("🌳 Green (#1b5e20)");
        colorCombo.Items.Add("🔵 Blue (#1976d2)");
        colorCombo.Items.Add("🟣 Purple (#7b1fa2)");
        colorCombo.Items.Add("🔴 Red (#c62828)");
        colorCombo.Items.Add("⚫ Dark (#212121)");
        colorCombo.SelectedIndex = 0;
        panel.Children.Add(colorCombo);
        
        // Reorder
        panel.Children.Add(new TextBlock { Text = "Reorder:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 15, 0, 0) });
        var reorderStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        
        moveUpBtn.Content = "⬆ Move Up";
        moveUpBtn.Padding = new Avalonia.Thickness(15, 5);
        moveUpBtn.Click += MoveUp_Click;
        
        moveDownBtn.Content = "⬇ Move Down";
        moveDownBtn.Padding = new Avalonia.Thickness(15, 5);
        moveDownBtn.Click += MoveDown_Click;
        
        reorderStack.Children.Add(moveUpBtn);
        reorderStack.Children.Add(moveDownBtn);
        panel.Children.Add(reorderStack);
        
        // Add/Delete
        panel.Children.Add(new TextBlock { Text = "Modify:", FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) });
        var modifyStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        
        addSepBtn.Content = "➕ Add Separator";
        addSepBtn.Padding = new Avalonia.Thickness(15, 5);
        addSepBtn.Click += AddSeparator_Click;
        
        deleteBtn.Content = "🗑 Delete Item";
        deleteBtn.Padding = new Avalonia.Thickness(15, 5);
        deleteBtn.Click += DeleteItem_Click;
        
        modifyStack.Children.Add(addSepBtn);
        modifyStack.Children.Add(deleteBtn);
        panel.Children.Add(modifyStack);
        
        return new Border 
        { 
            Child = panel,
            Background = new SolidColorBrush(Color.Parse("#fafafa"))
        };
    }
    
    private void TreeSelectionChanged(TreeView tree)
    {
        if (tree.SelectedItem is TreeViewItem item)
        {
            headerBox.Text = item.Header?.ToString() ?? "";
            statusText.Text = $"Selected: {item.Header}";
        }
    }
    
    private void LoadCurrentMenus()
    {
        LoadMainMenu();
        LoadContextMenu();
        statusText.Text = "✓ Menus loaded - select items to edit";
    }
    
    private void LoadMainMenu()
    {
        menuTree.Items.Clear();
        var menu = parentWindow.GetCurrentMenu();
        if (menu == null) return;
        
        var root = new TreeViewItem { Header = "Main Menu", IsExpanded = true };
        
        foreach (MenuItem topItem in menu.Items)
        {
            var topNode = new TreeViewItem { Header = topItem.Header, IsExpanded = true, Tag = topItem };
            
            foreach (var subItem in topItem.Items)
            {
                if (subItem is MenuItem mi)
                    topNode.Items.Add(new TreeViewItem { Header = mi.Header, Tag = mi });
                else if (subItem is Separator)
                    topNode.Items.Add(new TreeViewItem { Header = "--- Separator ---", Tag = subItem });
            }
            
            root.Items.Add(topNode);
        }
        
        menuTree.Items.Add(root);
    }
    
    private void LoadContextMenu()
    {
        contextTree.Items.Clear();
        var menu = parentWindow.GetCurrentContextMenu();
        if (menu == null) return;
        
        var root = new TreeViewItem { Header = "Context Menu", IsExpanded = true };
        
        foreach (var item in menu.Items)
        {
            if (item is MenuItem mi)
                root.Items.Add(new TreeViewItem { Header = mi.Header, Tag = mi });
            else if (item is Separator)
                root.Items.Add(new TreeViewItem { Header = "--- Separator ---", Tag = item });
        }
        
        contextTree.Items.Add(root);
    }
    
    private void MenuTemplate_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (menuTemplateCombo.SelectedItem == null) return;
        
        var template = menuTemplateCombo.SelectedItem.ToString() ?? "Basic";
        parentWindow.MenuTemplate = template;
        parentWindow.ApplyMenuTemplate();
        LoadMainMenu();
        
        statusText.Text = $"✓ Menu template: {template}";
    }
    
    private void ContextTemplate_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (contextTemplateCombo.SelectedItem == null) return;
        
        var template = contextTemplateCombo.SelectedItem.ToString() ?? "Basic";
        parentWindow.ContextMenuTemplate = template;
        parentWindow.ApplyContextMenuTemplate();
        LoadContextMenu();
        
        statusText.Text = $"✓ Context menu: {template}";
    }
    
    private void MoveUp_Click(object? sender, RoutedEventArgs e)
    {
        statusText.Text = "⬆ Move Up - Coming soon!";
    }
    
    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        statusText.Text = "⬇ Move Down - Coming soon!";
    }
    
    private void AddSeparator_Click(object? sender, RoutedEventArgs e)
    {
        statusText.Text = "➕ Add Separator - Coming soon!";
    }
    
    private void DeleteItem_Click(object? sender, RoutedEventArgs e)
    {
        statusText.Text = "🗑 Delete - Coming soon!";
    }
    
    private void ApplyTemplates(object? sender, RoutedEventArgs e)
    {
        parentWindow.ApplyMenuTemplate();
        parentWindow.ApplyContextMenuTemplate();
        DesignerWindow.LoadAndApply(parentWindow, "designer.vml");
        statusText.Text = "✓ Applied! Close window to see changes.";
    }
    
    private void SaveToVML(object? sender, RoutedEventArgs e)
    {
        statusText.Text = "💾 Save to VML - Coming soon!";
    }
}

