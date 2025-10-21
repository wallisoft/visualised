using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConfigUI
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, Control> _controls = new Dictionary<string, Control>();
        private Dictionary<string, DispatcherTimer> _timers = new Dictionary<string, DispatcherTimer>();
        private string? _yamlPath;
        
        // Drag support fields
        private Control? _draggedControl = null;
        private Point _dragStartPoint;
        private Point _dragStartControlPosition;

        public MainWindow() : this(null)
        {
        }

public MainWindow(string? yamlPath)
{
    InitializeComponent();
    _yamlPath = yamlPath;
    
    var splash = this.FindControl<Border>("SplashScreen");
    var canvas = this.FindControl<Canvas>("MainCanvas");
    
    if (!string.IsNullOrEmpty(_yamlPath) && File.Exists(_yamlPath))
    {
        if (splash != null) splash.IsVisible = false;
        if (canvas != null) canvas.IsVisible = true;
        
        var fileName = Path.GetFileName(_yamlPath);
        if (fileName == "visual-designer.yaml" || fileName == "designer.yaml")
        {
            LoadAndRenderYaml(_yamlPath);
        }
        else
        {
            bool showSplash = ShouldShowLoadingSplash(_yamlPath);
            
            if (showSplash)
            {
                var splashPath = FindYamlFile("splash.yaml");
                if (splashPath != null)
                {
                    LoadAndRenderYaml(splashPath);
                    Task.Delay(2000).ContinueWith(_ =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            LoadAndRenderYaml(_yamlPath);
                        });
                    });
                }
                else
                {
                    LoadAndRenderYaml(_yamlPath);
                }
            }
            else
            {
                LoadAndRenderYaml(_yamlPath);
            }
        }
    }
    else
    {
        if (splash != null) splash.IsVisible = false;
        if (canvas != null) canvas.IsVisible = true;
        
        var menuPath = FindYamlFile("menu.yaml");
        if (menuPath != null)
        {
            LoadAndRenderYaml(menuPath);
        }
        else
        {
            Console.WriteLine("⚠️ menu.yaml not found - showing empty window");
        }
    }
}

private bool ShouldShowLoadingSplash(string yamlPath)
{
    try
    {
        var yamlContent = File.ReadAllText(yamlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        
        var config = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
        
        if (config.ContainsKey("window"))
        {
            var window = config["window"] as Dictionary<object, object>;
            if (window != null && window.ContainsKey("show_loading_splash"))
            {
                var showSplash = window["show_loading_splash"];
                if (showSplash is bool boolValue)
                {
                    Console.WriteLine($"🎬 show_loading_splash = {boolValue}");
                    return boolValue;
                }
                if (showSplash?.ToString()?.ToLower() == "false")
                {
                    Console.WriteLine($"🎬 show_loading_splash = false");
                    return false;
                }
            }
        }
        
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error checking show_loading_splash: {ex.Message}");
        return true;
    }
}

        private string? FindYamlFile(string filename)
        {
            if (File.Exists(filename))
            {
                Console.WriteLine($"✅ Found {filename} in current directory");
                return filename;
            }
            
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var exePath = Path.Combine(exeDir, filename);
            if (File.Exists(exePath))
            {
                Console.WriteLine($"✅ Found {filename} in executable directory");
                return exePath;
            }
            
            Console.WriteLine($"⚠️ {filename} not found");
            return null;
        }

        private void LoadAndRenderYaml(string yamlPath)
        {
            try
            {
                var yamlContent = File.ReadAllText(yamlPath);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                Dictionary<string, object> config;
                
                try
                {
                    config = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                }
                catch (YamlDotNet.Core.YamlException yamlEx)
                {
                    var errorMsg = $"YAML Parsing Error:\n\n" +
                                  $"File: {Path.GetFileName(yamlPath)}\n" +
                                  $"Line: {yamlEx.Start.Line}\n" +
                                  $"Column: {yamlEx.Start.Column}\n\n" +
                                  $"Error: {yamlEx.Message}\n\n" +
                                  $"Check your YAML syntax around line {yamlEx.Start.Line}";
                    ShowError("YAML Parse Error", errorMsg);
                    return;
                }

                if (config.ContainsKey("window"))
                {
                    var window = config["window"] as Dictionary<object, object>;
                    if (window != null)
                    {
                        if (window.ContainsKey("width"))
                            Width = Convert.ToDouble(window["width"]);
                        if (window.ContainsKey("height"))
                            Height = Convert.ToDouble(window["height"]);
                        if (window.ContainsKey("title"))
                            Title = window["title"].ToString();
                        
                        if (window.ContainsKey("background_image"))
                        {
                            var bgImage = window["background_image"].ToString();
                            var bgOpacity = window.ContainsKey("background_opacity") ? Convert.ToDouble(window["background_opacity"]) : 1.0;
                            
                            try
                            {
                                if (File.Exists(bgImage))
                                {
                                    var bitmap = new Avalonia.Media.Imaging.Bitmap(bgImage);
                                    var imageBrush = new Avalonia.Media.ImageBrush(bitmap)
                                    {
                                        Stretch = Avalonia.Media.Stretch.UniformToFill,
                                        Opacity = bgOpacity
                                    };
                                    this.Background = imageBrush;
                                    Console.WriteLine($"✅ Window background applied");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ Window background error: {ex.Message}");
                            }
                        }
                        else if (window.ContainsKey("background_color"))
                        {
                            var bgColor = window["background_color"].ToString();
                            this.Background = new SolidColorBrush(Color.Parse(bgColor));
                            Console.WriteLine($"✅ Applied window background color: {bgColor}");
                        }
                    }
                }

                var canvas = this.FindControl<Canvas>("MainCanvas");
                if (canvas == null)
                {
                    ShowError("Canvas Not Found", "MainCanvas not found in XAML. Cannot render controls.");
                    return;
                }

                canvas.Children.Clear();
                _controls.Clear();
                _timers.Clear();

                if (config.ContainsKey("controls"))
                {
                    var controlsList = config["controls"] as List<object>;
                    if (controlsList != null)
                    {
                        foreach (var item in controlsList)
                        {
                            var controlData = item as Dictionary<object, object>;
                            if (controlData != null)
                            {
                                CreateControl(canvas, controlData);
                            }
                        }
                    }
                }

                Console.WriteLine($"✅ Loaded and rendered: {Path.GetFileName(yamlPath)}");
                Console.WriteLine($"   Controls created: {_controls.Count}");
            }
            catch (Exception ex)
            {
                ShowError("YAML Loading Error", $"Failed to load YAML:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}");
            }
        }

private void CreateControl(Canvas canvas, Dictionary<object, object> data)
{
    try
    {
        var type = data.ContainsKey("type") ? data["type"].ToString() : "label";
        var name = data.ContainsKey("name") ? data["name"].ToString() : $"Control_{_controls.Count}";
        var x = data.ContainsKey("x") ? Convert.ToDouble(data["x"]) : 0;
        var y = data.ContainsKey("y") ? Convert.ToDouble(data["y"]) : 0;
        var width = data.ContainsKey("width") ? Convert.ToDouble(data["width"]) : 100;
        var height = data.ContainsKey("height") ? Convert.ToDouble(data["height"]) : 30;

        Control control = null;

        switch (type?.ToLower())
        {
            case "label":
                control = CreateLabel(data);
                break;

            case "button":
                control = CreateButton(data);
                break;

            case "textbox":
                control = CreateTextBox(data);
                break;

            case "checkbox":
                control = CreateCheckBox(data);
                break;

            case "combobox":
                control = CreateComboBox(data);
                break;

            case "radiobutton":
                control = CreateRadioButton(data);
                break;

            case "listbox":
                control = CreateListBox(data);
                break;

            case "panel":
                control = CreatePanel(data);
                break;

            case "timer":
                CreateTimer(name, data);
                return;

            case "progressbar":
                control = CreateProgressBar(data);
                break;

            case "menubar":
                control = CreateMenuBar(data);
                break;

            case "toolbar":
                control = CreateToolBar(data);
                break;

            case "tabcontrol":
                control = CreateTabControl(data);
                break;

            default:
                var customDef = LoadCustomControl(type);
                if (customDef != null)
                {
                    control = CreateCustomControl(customDef, data);
                }
                else
                {
                    Console.WriteLine($"⚠️ Unknown control type: {type}");
                    return;
                }
                break;
        }

        if (control != null)
        {
            control.Name = name;
            
            if (data.ContainsKey("visible"))
            {
                var visible = data["visible"];
                if (visible is bool boolValue)
                {
                    control.IsVisible = boolValue;
                }
                else if (visible?.ToString()?.ToLower() == "false")
                {
                    control.IsVisible = false;
                }
            }
            
            if (data.ContainsKey("draggable") && Convert.ToBoolean(data["draggable"]))
            {
                MakeDraggable(control, canvas);
            }
            
            if (data.ContainsKey("background_image") && type?.ToLower() != "panel")
            {
                var bgImage = data["background_image"].ToString();
                control = ApplyBackgroundImage(control, bgImage);
            }
            
            control.Width = width;
            control.Height = height;
            Canvas.SetLeft(control, x);
            Canvas.SetTop(control, y);

            canvas.Children.Add(control);
            _controls[name] = control;

            Console.WriteLine($"✅ Created {type}: {name} at ({x}, {y}) size {width}x{height}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error creating control: {ex.Message}");
    }
}
private Control CreateLabel(Dictionary<object, object> data)
{
    var caption = data.ContainsKey("caption") ? data["caption"].ToString() : "Label";
    var fontSize = data.ContainsKey("font_size") ? Convert.ToDouble(data["font_size"]) : 12;
    var fontBold = data.ContainsKey("font_bold") && Convert.ToBoolean(data["font_bold"]);
    var fgColor = data.ContainsKey("foreground_color") ? Color.Parse(data["foreground_color"].ToString()!) : Colors.Black;
    var alignment = data.ContainsKey("alignment") ? data["alignment"].ToString()?.ToLower() : "left";

    var textAlignment = alignment switch
    {
        "center" => TextAlignment.Center,
        "right" => TextAlignment.Right,
        _ => TextAlignment.Left
    };

    return new TextBlock
    {
        Text = caption,
        FontSize = fontSize,
        FontWeight = fontBold ? FontWeight.Bold : FontWeight.Normal,
        Foreground = new SolidColorBrush(fgColor),
        TextAlignment = textAlignment,
        VerticalAlignment = VerticalAlignment.Center,
        Padding = new Thickness(5)
    };
}

private Control CreateButton(Dictionary<object, object> data)
{
    var caption = data.ContainsKey("caption") ? data["caption"].ToString() : "Button";
    var button = new Button
    {
        Content = caption,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center
    };

    if (data.ContainsKey("onClick"))
    {
        var script = data["onClick"].ToString();
        button.Click += async (s, e) =>
        {
            await ExecuteScript(script!);
        };
    }

    return button;
}

private Control CreateTextBox(Dictionary<object, object> data)
{
    var text = data.ContainsKey("text") ? data["text"].ToString() : "";
    var placeholder = data.ContainsKey("placeholder") ? data["placeholder"].ToString() : "";
    var multiline = data.ContainsKey("multiline") && Convert.ToBoolean(data["multiline"]);
    var readonly_ = data.ContainsKey("readonly") && Convert.ToBoolean(data["readonly"]);
    var fontFamily = data.ContainsKey("font_family") ? data["font_family"].ToString() : null;
    var fontSize = data.ContainsKey("font_size") ? Convert.ToDouble(data["font_size"]) : 12;

    var textBox = new TextBox
    {
        Text = text,
        Watermark = placeholder,
        AcceptsReturn = multiline,
        TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
        IsReadOnly = readonly_,
        FontSize = fontSize
    };

    if (!string.IsNullOrEmpty(fontFamily))
    {
        textBox.FontFamily = new FontFamily(fontFamily);
    }

    return textBox;
}

private Control CreateCheckBox(Dictionary<object, object> data)
{
    var caption = data.ContainsKey("caption") ? data["caption"].ToString() : "CheckBox";
    var isChecked = data.ContainsKey("checked") && Convert.ToBoolean(data["checked"]);

    return new CheckBox
    {
        Content = caption,
        IsChecked = isChecked
    };
}

private Control CreateComboBox(Dictionary<object, object> data)
{
    var comboBox = new ComboBox();

    if (data.ContainsKey("items") && data["items"] is List<object> items)
    {
        foreach (var item in items)
        {
            comboBox.Items.Add(item.ToString());
        }
    }

    if (data.ContainsKey("selected_index"))
    {
        comboBox.SelectedIndex = Convert.ToInt32(data["selected_index"]);
    }

    return comboBox;
}

private Control CreateRadioButton(Dictionary<object, object> data)
{
    var caption = data.ContainsKey("caption") ? data["caption"].ToString() : "RadioButton";
    var isChecked = data.ContainsKey("checked") && Convert.ToBoolean(data["checked"]);
    var groupName = data.ContainsKey("group") ? data["group"].ToString() : null;

    return new RadioButton
    {
        Content = caption,
        IsChecked = isChecked,
        GroupName = groupName
    };
}

private Control CreateListBox(Dictionary<object, object> data)
{
    var listBox = new ListBox();

    if (data.ContainsKey("items") && data["items"] is List<object> items)
    {
        foreach (var item in items)
        {
            listBox.Items.Add(item.ToString());
        }
    }

    if (data.ContainsKey("selected_index"))
    {
        listBox.SelectedIndex = Convert.ToInt32(data["selected_index"]);
    }

    return listBox;
}

private Control CreatePanel(Dictionary<object, object> data)
{
    var bgColor = data.ContainsKey("background_color")
        ? Color.Parse(data["background_color"].ToString()!)
        : Colors.LightGray;

    var borderColor = data.ContainsKey("border_color")
        ? Color.Parse(data["border_color"].ToString()!)
        : Colors.Black;

    var borderThickness = data.ContainsKey("border_thickness")
        ? Convert.ToDouble(data["border_thickness"])
        : 0;

    var panel = new Canvas
    {
        Background = new SolidColorBrush(bgColor)
    };

    var border = new Border
    {
        Child = panel,
        Background = new SolidColorBrush(bgColor),
        BorderBrush = new SolidColorBrush(borderColor),
        BorderThickness = new Thickness(borderThickness)
    };

    if (data.ContainsKey("controls") && data["controls"] is List<object> controls)
    {
        foreach (var controlObj in controls)
        {
            if (controlObj is Dictionary<object, object> controlData)
            {
                CreateControl(panel, controlData);
            }
        }
    }

    return border;
}

private Control CreateProgressBar(Dictionary<object, object> data)
{
    var value = data.ContainsKey("value") ? Convert.ToDouble(data["value"]) : 0;
    var minimum = data.ContainsKey("minimum") ? Convert.ToDouble(data["minimum"]) : 0;
    var maximum = data.ContainsKey("maximum") ? Convert.ToDouble(data["maximum"]) : 100;

    return new ProgressBar
    {
        Value = value,
        Minimum = minimum,
        Maximum = maximum
    };
}

private Control CreateMenuBar(Dictionary<object, object> data)
{
    var menu = new Menu();

    if (data.ContainsKey("items") && data["items"] is List<object> items)
    {
        foreach (var itemObj in items)
        {
            if (itemObj is Dictionary<object, object> itemData)
            {
                var menuItem = CreateMenuItem(itemData);
                if (menuItem != null)
                {
                    menu.Items.Add(menuItem);
                }
            }
        }
    }

    return menu;
}

private MenuItem? CreateMenuItem(Dictionary<object, object> data)
{
    var header = data.ContainsKey("header") ? data["header"].ToString() : "";

    if (header == "---")
    {
        return new MenuItem { Header = "-" };
    }

    var menuItem = new MenuItem { Header = header };

    if (data.ContainsKey("items") && data["items"] is List<object> subItems)
    {
        foreach (var subItemObj in subItems)
        {
            if (subItemObj is Dictionary<object, object> subItemData)
            {
                var subMenuItem = CreateMenuItem(subItemData);
                if (subMenuItem != null)
                {
                    menuItem.Items.Add(subMenuItem);
                }
            }
        }
    }

    return menuItem;
}

private Control CreateToolBar(Dictionary<object, object> data)
{
    return new TextBlock { Text = "[ToolBar - Not yet implemented]" };
}

private Control CreateTabControl(Dictionary<object, object> data)
{
    return new TextBlock { Text = "[TabControl - Not yet implemented]" };
}

    private void CreateTimer(string name, Dictionary<object, object> data)
    {
        var interval = data.ContainsKey("interval") ? Convert.ToInt32(data["interval"]) : 1000;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(interval) };
        
        if (data.ContainsKey("onTick"))
        {
            var script = data["onTick"].ToString();
            timer.Tick += async (s, e) => await ExecuteScript(script!);
        }
        
        _timers[name] = timer;
        Console.WriteLine($"✅ Created timer: {name} with interval {interval}ms");
    }

    private Dictionary<object, object>? LoadCustomControl(string? type)
    {
        return null;
    }

    private Control CreateCustomControl(Dictionary<object, object> definition, Dictionary<object, object> data)
    {
        return new TextBlock { Text = "[Custom Control]" };
    }

    private Control ApplyBackgroundImage(Control control, string imagePath)
    {
        return control;
    }

    private void ShowError(string title, string message)
    {
        Console.WriteLine($"❌ {title}: {message}");
    }

    private void MakeDraggable(Control control, Canvas canvas)
    {
        // Draggable functionality disabled in regular MainWindow
        // Only used in DesignerWindow
    }

    private async Task ExecuteScript(string script)
    {
        try
        {
            var tempFile = Path.GetTempFileName() + ".sh";
            await File.WriteAllTextAsync(tempFile, script);
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = tempFile,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
	            foreach (var kvp in _controls)
        {
            var controlName = kvp.Key;
            var control = kvp.Value;

            try
            {
                if (control is TextBox tb)
                {
                    process.StartInfo.Environment[$"ctrl_{controlName}"] = tb.Text ?? "";
                }
                else if (control is TextBlock label)
                {
                    process.StartInfo.Environment[$"ctrl_{controlName}"] = label.Text ?? "";
                }
                else if (control is CheckBox cb)
                {
                    process.StartInfo.Environment[$"ctrl_{controlName}"] = cb.IsChecked?.ToString() ?? "false";
                }
                else if (control is Button btn)
                {
                    process.StartInfo.Environment[$"ctrl_{controlName}"] = btn.Content?.ToString() ?? "";
                }
                else if (control is ComboBox combo)
                {
                    process.StartInfo.Environment[$"ctrl_{controlName}"] = combo.SelectedItem?.ToString() ?? "";
                }
                else if (control is RadioButton radio)
                {
                    process.StartInfo.Environment[$"ctrl_{controlName}"] = radio.IsChecked?.ToString() ?? "false";
                }
                else if (control is ListBox listBox)
                {
                    process.StartInfo.Environment[$"ctrl_{controlName}"] = listBox.SelectedItem?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not export {controlName}: {ex.Message}");
            }
        }

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            if (!string.IsNullOrEmpty(output))
            {
                ProcessScriptOutput(output);
            }
            
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Script Error: {error}");
            }
            
            File.Delete(tempFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Script execution error: {ex.Message}");
        }
    }

    private void ProcessScriptOutput(string output)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("__CLOSE_WINDOW__"))
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => this.Close());
            }
	            else if (line.StartsWith("__SET__"))
        {
            try
            {
                // Format: __SET__ controlName propertyName value
                var parts = line.Substring(7).Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var controlName = parts[0];
                    var propertyName = parts[1];
                    var value = parts[2].Trim('\'', '"'); // Remove quotes

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        SetControlProperty(controlName, propertyName, value);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to parse __SET__ command: {ex.Message}");
            }
        }
            else
            {
                Console.WriteLine(line);
            }
        }
    }
    private void SetControlProperty(string controlName, string propertyName, string value)
    {
        if (!_controls.ContainsKey(controlName))
        {
            Console.WriteLine($"⚠️ Control not found: {controlName}");
            return;
        }
        
        var control = _controls[controlName];
        
        try
        {
            switch (propertyName.ToLower())
            {
                case "text":
                    if (control is TextBox tb)
                        tb.Text = value;
                    else if (control is TextBlock label)
                        label.Text = value;
                    else if (control is Button btn)
                        btn.Content = value;
                    Console.WriteLine($"✅ Set {controlName}.Text = '{value}'");
                    break;
                    
                case "caption":
                    if (control is TextBlock label2)
                        label2.Text = value;
                    else if (control is Button btn2)
                        btn2.Content = value;
                    Console.WriteLine($"✅ Set {controlName}.Caption = '{value}'");
                    break;
                    
                case "visible":
                    control.IsVisible = bool.Parse(value);
                    Console.WriteLine($"✅ Set {controlName}.Visible = {value}");
                    break;
                    
                case "enabled":
                    control.IsEnabled = bool.Parse(value);
                    Console.WriteLine($"✅ Set {controlName}.Enabled = {value}");
                    break;
                    
                case "checked":
                    if (control is CheckBox cb)
                        cb.IsChecked = bool.Parse(value);
                    else if (control is RadioButton rb)
                        rb.IsChecked = bool.Parse(value);
                    Console.WriteLine($"✅ Set {controlName}.Checked = {value}");
                    break;
                    
                case "selectedindex":
                    if (control is ComboBox combo)
                        combo.SelectedIndex = int.Parse(value);
                    else if (control is ListBox listBox)
                        listBox.SelectedIndex = int.Parse(value);
                    Console.WriteLine($"✅ Set {controlName}.SelectedIndex = {value}");
                    break;
                    
                default:
                    Console.WriteLine($"⚠️ Unknown property: {propertyName}");
                    break;
            }               
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to set {controlName}.{propertyName}: {ex.Message}");
        }
    }
    }
}
    