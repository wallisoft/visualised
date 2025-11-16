using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.VisualTree; 
using Microsoft.Data.Sqlite;

namespace VB;

/// <summary>
/// TinyMenu - Completely VML-driven menu system
/// Reads menu structure, theme, and handlers from database
/// </summary>
public class TinyMenu : Border
{
    private readonly string _dbPath;
    private Grid _menuBar;
    private List<MenuItemData> _menuItems = new();
    private Border? _activePopup;
    private MenuTheme _theme;
    
    public TinyMenu(string dbPath)
    {
        _dbPath = dbPath;
        LoadTheme();
        LoadMenuStructure();
        BuildUI();
    }
    
    private void LoadTheme()
    {
        _theme = new MenuTheme
        {
            Background = GetProperty("MenuTheme", "Background") ?? "#107C10",
            Foreground = GetProperty("MenuTheme", "Foreground") ?? "White",
            HoverBackground = GetProperty("MenuTheme", "HoverBackground") ?? "White",
            HoverForeground = GetProperty("MenuTheme", "HoverForeground") ?? "#107C10",
            PopupBackground = GetProperty("MenuTheme", "PopupBackground") ?? "White",
            PopupBorder = GetProperty("MenuTheme", "PopupBorder") ?? "#107C10",
            Height = double.Parse(GetProperty("MenuTheme", "Height") ?? "30")
        };
    }
    
    private void LoadMenuStructure()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        
        // Get all MenuItem objects
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT ut.id, ut.name, ut.parent_id
            FROM ui_tree ut
            WHERE ut.control_type = 'MenuItem'
            ORDER BY ut.display_order";
        
        using var reader = cmd.ExecuteReader();
        var items = new List<(int id, string name, int? parentId)>();
        
        while (reader.Read())
        {
            items.Add((
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2)
            ));
        }
        reader.Close();
        
        // Load properties for each menu item
        foreach (var (id, name, parentId) in items)
        {
            var menuItem = new MenuItemData
            {
                Id = id,
                Name = name,
                ParentId = parentId,
                Text = GetPropertyById(id, "Text") ?? name,
                Shortcut = GetPropertyById(id, "Shortcut"),
                OnClick = GetPropertyById(id, "OnClick")
            };
            
            _menuItems.Add(menuItem);
        }
        
        Console.WriteLine($"[TINYMENU] Loaded {_menuItems.Count} menu items");
    }
    
    private void BuildUI()
    {
        _menuBar = new Grid
        {
            Background = Brush.Parse(_theme.Background),
            Height = _theme.Height
        };
        
        // Build top-level menu buttons (items with no parent or parent = MenuBar)
        var topLevel = _menuItems.Where(m => 
            m.ParentId == null || 
            GetParentName(m.ParentId.Value) == "MenuBar").ToList();
        
        _menuBar.ColumnDefinitions = new ColumnDefinitions(
            string.Join(",", topLevel.Select(_ => "Auto")) + ",*"
        );
        
        for (int i = 0; i < topLevel.Count; i++)
        {
            var menuItem = topLevel[i];
            var button = CreateTopLevelButton(menuItem);
            Grid.SetColumn(button, i);
            _menuBar.Children.Add(button);
            
            Console.WriteLine($"[TINYMENU] Created top-level: {menuItem.Text}");
        }
        
        Child = _menuBar;
    }
    
    private Button CreateTopLevelButton(MenuItemData menuItem)
    {
        var button = new Button
        {
            Content = menuItem.Text,
            Background = Brushes.Transparent,
            Foreground = Brush.Parse(_theme.Foreground),
            BorderThickness = new Thickness(0),
            FontWeight = FontWeight.Bold,
            FontSize = 13,
            Padding = new Thickness(15, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        
        // Hover effect
        button.PointerEntered += (s, e) =>
        {
            button.Background = Brush.Parse(_theme.HoverBackground);
            button.Foreground = Brush.Parse(_theme.HoverForeground);
        };
        
        button.PointerExited += (s, e) =>
        {
            if (_activePopup == null || _activePopup.Tag != menuItem)
            {
                button.Background = Brushes.Transparent;
                button.Foreground = Brush.Parse(_theme.Foreground);
            }
        };
        
        // Click - show popup
        button.Click += (s, e) =>
        {
            if (_activePopup?.Tag == menuItem)
            {
                ClosePopup();
            }
            else
            {
                ShowPopup(menuItem, button);
            }
        };
        
        return button;
    }
    
    private void ShowPopup(MenuItemData menuItem, Button parentButton)
    {
        ClosePopup();
        
        // Get child items
        var children = _menuItems.Where(m => m.ParentId == menuItem.Id).ToList();
        if (children.Count == 0) return;
        
        var stack = new StackPanel { Spacing = 0 };
        
        foreach (var child in children)
        {
            var itemButton = new Button
            {
                Content = child.Text + (child.Shortcut != null ? $"    {child.Shortcut}" : ""),
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(15, 8),
                FontSize = 12,
                MinWidth = 200,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            
            // Hover - use theme colors from VML
            itemButton.PointerEntered += (s, e) =>
            {
                itemButton.Background = Brush.Parse(_theme.HoverBackground);
                itemButton.Foreground = Brush.Parse(_theme.HoverForeground);
            };
            
            itemButton.PointerExited += (s, e) =>
            {
                itemButton.Background = Brushes.Transparent;
                itemButton.Foreground = Brushes.Black;
            };
            
            // Click - execute action
            itemButton.Click += (s, e) =>
            {
                ClosePopup();
                if (!string.IsNullOrEmpty(child.OnClick))
                {
                    ExecuteMenuAction(child.OnClick);
                }
            };
            
            stack.Children.Add(itemButton);
        }
        
        // Create popup border (ONLY ONCE)
        _activePopup = new Border
        {
            Background = Brush.Parse(_theme.PopupBackground),
            BorderBrush = Brush.Parse(_theme.PopupBorder),
            BorderThickness = new Thickness(1),
            BoxShadow = new BoxShadows(new BoxShadow { Blur = 10, Color = Colors.Black, OffsetY = 2 }),
            Child = stack,
            Tag = menuItem,
            ZIndex = 1000
        };
        
        // Position and add to MainGrid
        var rootGrid = FindRootGrid();
        if (rootGrid != null)
        {
            var buttonPos = parentButton.TranslatePoint(new Point(0, parentButton.Bounds.Height), rootGrid);
            if (buttonPos.HasValue)
            {
                _activePopup.Margin = new Thickness(buttonPos.Value.X, buttonPos.Value.Y, 0, 0);
            }
            
            if (!rootGrid.Children.Contains(_activePopup))
            {
                rootGrid.Children.Add(_activePopup);
            }
        }
    }
        
        private void ClosePopup()
        {
            if (_activePopup == null) return;
            
            var rootGrid = FindRootGrid();
            if (rootGrid != null && rootGrid.Children.Contains(_activePopup))
            {
                rootGrid.Children.Remove(_activePopup);
            }
        
        // Fully clear children to release parent relationships
        if (_activePopup.Child is Panel panel)
        {
            panel.Children.Clear();
        }
        _activePopup.Child = null;
        _activePopup = null;
    }

    private void ExecuteMenuAction(string scriptName)
    {
        Console.WriteLine($"[TINYMENU] Executing: {scriptName}");

        var script = ScriptRegistry.Get(scriptName);
        if (script != null)
        {
            ScriptHandler.Execute(script.Content, script.Interpreter, null);  // Add null for args
        }
        else
        {
            Console.WriteLine($"[TINYMENU] Script not found in registry: {scriptName}");
        }
    }
    
    private string? GetProperty(string objectName, string propertyName)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT up.property_value
            FROM ui_tree ut
            JOIN ui_properties up ON ut.id = up.ui_tree_id
            WHERE ut.name = @name AND up.property_name = @prop";
        cmd.Parameters.AddWithValue("@name", objectName);
        cmd.Parameters.AddWithValue("@prop", propertyName);
        
        return cmd.ExecuteScalar()?.ToString();
    }
    
    private string? GetPropertyById(int id, string propertyName)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT property_value FROM ui_properties WHERE ui_tree_id = @id AND property_name = @prop";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@prop", propertyName);
        
        return cmd.ExecuteScalar()?.ToString();
    }
    
    private string GetParentName(int parentId)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM ui_tree WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", parentId);
        
        return cmd.ExecuteScalar()?.ToString() ?? "";
    }

    private Grid? FindRootGrid()
    {
        // Walk up the visual tree to find MainGrid
        var current = this.Parent;
        while (current != null)
        {
            if (current is Grid grid && grid.Name == "MainGrid")
                return grid;

            if (current is Visual visual)
                current = visual.GetVisualParent();
            else
                break;
        }
        return null;
    }
}

public class MenuItemData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? ParentId { get; set; }
    public string Text { get; set; } = "";
    public string? Shortcut { get; set; }
    public string? OnClick { get; set; }
}

public class MenuTheme
{
    public string Background { get; set; } = "#107C10";
    public string Foreground { get; set; } = "White";
    public string HoverBackground { get; set; } = "White";
    public string HoverForeground { get; set; } = "#107C10";
    public string PopupBackground { get; set; } = "White";
    public string PopupBorder { get; set; } = "#107C10";
    public double Height { get; set; } = 30;
}
