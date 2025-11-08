using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;

using Avalonia.Interactivity;
namespace VB;

public class ControlBuilder
{
    private List<VmlControl> vmlControls;
    private Dictionary<string, Control> controls = new();
    
    public ControlBuilder(List<VmlControl> vmlControls)
    {
        this.vmlControls = vmlControls;
    }
    
    public Window? BuildWindow()
    {
        CreateAllControls();
        BuildHierarchy();
        WireEvents();
        
        return controls.Values.OfType<Window>().FirstOrDefault();
    }

    public List<Control> BuildControls()
    {
        CreateAllControls();
        BuildHierarchy();
        WireEvents();
        
        var result = new List<Control>();
        foreach (var control in controls.Values)
        {
            if (!(control is Window))
                result.Add(control);
        }
        
        Console.WriteLine($"[BUILDER] Built {result.Count} controls for designer");
        return result;
    }
    
    private void CreateAllControls()
    {
        foreach (var vmlControl in vmlControls)
        {
            var control = CreateControl(vmlControl);
            if (control != null && vmlControl.Name != null)
            {
                controls[vmlControl.Name] = control;
            }
        }
    }
    
    private Control? CreateControl(VmlControl vmlControl)
    {
        Control? control = null;
        
        if (vmlControl.Type == "Window")
        {
            control = new Window();
        }
        else
        {
            var fullTypeName = $"Avalonia.Controls.{vmlControl.Type}, Avalonia.Controls";
            var controlType = Type.GetType(fullTypeName);
            
            if (controlType == null)
            {
                fullTypeName = $"Avalonia.Controls.{vmlControl.Type}";
                controlType = Type.GetType(fullTypeName);
            }
            
            if (controlType == null || !typeof(Control).IsAssignableFrom(controlType))
                return null;
                
            control = Activator.CreateInstance(controlType) as Control;
        }
        
        if (control == null) return null;
        
        // SET THE NAME - CRITICAL FOR FindControl!
        if (vmlControl.Name != null && !(control is MainWindow))
        {
            control.Name = vmlControl.Name;
            Console.WriteLine($"[CREATE] Set Name={vmlControl.Name} on {control.GetType().Name}");
        }
        
        // Apply properties
        foreach (var prop in vmlControl.Properties)
        {
            if (prop.Key == "Parent") continue;
            if (prop.Key.Contains('.')) continue;
            if (prop.Key == "Name") continue;
            if (prop.Key.StartsWith("On")) continue;
            
            SetProperty(control, prop.Key, prop.Value);
        }
        
        return control;
    }
    
    private void BuildHierarchy()
    {
        Console.WriteLine("[BUILD] Starting BuildHierarchy");
        
        foreach (var vmlControl in vmlControls)
        {
            if (!vmlControl.Properties.ContainsKey("Parent")) continue;
            if (vmlControl.Name == null || !controls.ContainsKey(vmlControl.Name)) continue;
            
            var child = controls[vmlControl.Name];
            var parentName = vmlControl.Properties["Parent"];
            
            Console.WriteLine($"[BUILD] Processing {vmlControl.Name} -> Parent: {parentName}");
            
            if (!controls.ContainsKey(parentName)) continue;
            
            var parent = controls[parentName];
            
            foreach (var prop in vmlControl.Properties)
            {
                if (prop.Key.Contains('.'))
                {
                    SetAttachedProperty(child, prop.Key, prop.Value);
                }
            }
            
            if (parent is Expander expander)
            {
                Console.WriteLine($"[BUILD] Setting Expander.Content to {child.GetType().Name}");
                expander.Content = child;
            }
            else if (parent is Menu menu)
            {
                Console.WriteLine($"[BUILD] Adding {child.GetType().Name} to Menu.Items");
                menu.Items.Add(child);
            }
            else if (parent is MenuItem menuItem)
            {
                Console.WriteLine($"[BUILD] Adding {child.GetType().Name} to MenuItem.Items");
                menuItem.Items.Add(child);
            }
            else if (parent is Panel panel)
            {
                Console.WriteLine($"[BUILD] Adding {child.GetType().Name} to Panel");
                panel.Children.Add(child);
            }
            else if (parent is Decorator decorator)
            {
                Console.WriteLine($"[BUILD] Setting Decorator.Child to {child.GetType().Name}");
                decorator.Child = child;
            }
            else if (parent is ContentControl contentControl)
            {
                Console.WriteLine($"[BUILD] Setting ContentControl.Content to {child.GetType().Name}");
                contentControl.Content = child;
            }
            else if (parent is Window window)
            {
                Console.WriteLine($"[BUILD] Setting Window.Content to {child.GetType().Name}");
                window.Content = child;
            }
        }
    }
    
    private void WireEvents()
    {
        Console.WriteLine("[WIRE] Starting event wiring");
        
        foreach (var vmlControl in vmlControls)
        {
            if (vmlControl.Name == null || !controls.ContainsKey(vmlControl.Name))
                continue;
                
            var control = controls[vmlControl.Name];
            
            foreach (var prop in vmlControl.Properties)
            {
                if (!prop.Key.StartsWith("On")) continue;
                
                var eventName = prop.Key.Substring(2);
                var handlerName = prop.Value;
                
                WireEvent(control, eventName, handlerName);
            }
        }
    }
    
    private void WireEvent(Control control, string eventName, string handlerName)
    {
        var eventInfo = control.GetType().GetEvent(eventName);
        if (eventInfo == null)
        {
            Console.WriteLine($"[WIRE-FAIL] Event {eventName} not found on {control.GetType().Name}");
            return;
        }
        
        // Check if handler is a script FIRST
        if (handlerName.Contains(":") || handlerName.EndsWith(".sh") || handlerName.Contains(" "))
        {
            try
            {
                EventHandler<RoutedEventArgs> handler = (s, e) => 
                {
                    ScriptExecutor.ExecuteScript(handlerName, control);
                };
                
                eventInfo.AddEventHandler(control, handler);
                Console.WriteLine($"[WIRE] {control.Name}.{eventName} -> SCRIPT: {handlerName}");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WIRE-FAIL] Script handler failed: {ex.Message}");
                return;
            }
        }
        
        // Otherwise look for C# method in MainWindow
        var mainWindow = controls.Values.OfType<MainWindow>().FirstOrDefault();
        if (mainWindow == null)
        {
            Console.WriteLine($"[WIRE-FAIL] MainWindow not found");
            return;
        }
        
        var handlerMethod = mainWindow.GetType().GetMethod(handlerName, 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (handlerMethod == null)
        {
            Console.WriteLine($"[WIRE-FAIL] Handler {handlerName} not found in MainWindow");
            return;
        }
        
        try
        {
            var delegateType = eventInfo.EventHandlerType;
            var eventHandler = Delegate.CreateDelegate(delegateType!, mainWindow, handlerMethod);
            eventInfo.AddEventHandler(control, eventHandler);
            Console.WriteLine($"[WIRE] {control.Name}.{eventName} -> {handlerName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WIRE-FAIL] {eventName} -> {handlerName}: {ex.Message}");
        }
    }
    
    private void SetAttachedProperty(Control control, string propertyName, string value)
    {
        var parts = propertyName.Split('.');
        if (parts.Length != 2) return;
        
        var className = parts[0];
        var propName = parts[1];
        
        var attachedType = Type.GetType($"Avalonia.Controls.{className}, Avalonia.Controls");
        if (attachedType == null) return;
        
        var setMethod = attachedType.GetMethod($"Set{propName}", BindingFlags.Public | BindingFlags.Static);
        if (setMethod == null) return;
        
        try
        {
            var paramType = setMethod.GetParameters()[1].ParameterType;
            var convertedValue = ConvertValue(value, paramType);
            Console.WriteLine($"[ATTACH] Setting {propertyName} = {value} on {control.GetType().Name}");
            setMethod.Invoke(null, new object[] { control, convertedValue });
            Console.WriteLine($"[ATTACH-SUCCESS] {propertyName} set on {control.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ATTACH-FAIL] {propertyName} failed: {ex.Message}");
        }
    }
    
    private void SetProperty(Control control, string name, string value)
    {
        var propInfo = control.GetType().GetProperty(name);
        if (propInfo != null && propInfo.CanWrite)
        {
            try
            {
                var convertedValue = ConvertValue(value, propInfo.PropertyType);
                Console.WriteLine($"[PROP] Setting {name} = {value} on {control.GetType().Name}");
                propInfo.SetValue(control, convertedValue);
            }
            catch
            {
            }
        }
    }
    
    private object? ConvertValue(string value, Type targetType)
    {
        if (value == null) return null;
        
        if (targetType == typeof(Avalonia.Media.IBrush))
        {
            return Avalonia.Media.Brush.Parse(value);
        }
        
        if (targetType == typeof(Avalonia.Media.Brush))
        {
            return Avalonia.Media.Brush.Parse(value);
        }
        
        if (targetType == typeof(Orientation))
        {
            return value == "Vertical" ? Orientation.Vertical : Orientation.Horizontal;
        }
        
        if (targetType == typeof(Avalonia.Thickness))
        {
            return ParseThickness(value);
        }
        
        if (targetType == typeof(Dock))
        {
            Console.WriteLine($"[CONVERT] Converting '{value}' to Dock enum");
            var result = Enum.Parse<Dock>(value);
            Console.WriteLine($"[CONVERT] Result: {result}");
            return result;
        }
        
        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value);
        }
        
        if (targetType == typeof(bool))
        {
            return bool.Parse(value);
        }
        
        if (targetType == typeof(int))
        {
            return int.Parse(value);
        }
        
        if (targetType == typeof(double))
        {
            return double.Parse(value);
        }
        
        return Convert.ChangeType(value, targetType);
    }
    
    private Avalonia.Thickness ParseThickness(string value)
    {
        var parts = value.Split(',');
        
        if (parts.Length == 1 && double.TryParse(parts[0], out var uniform))
        {
            return new Avalonia.Thickness(uniform);
        }
        else if (parts.Length == 4 &&
                 double.TryParse(parts[0], out var left) &&
                 double.TryParse(parts[1], out var top) &&
                 double.TryParse(parts[2], out var right) &&
                 double.TryParse(parts[3], out var bottom))
        {
            return new Avalonia.Thickness(left, top, right, bottom);
        }
        
        return new Avalonia.Thickness(0);
    }

    public Control? GetControl(string name)
    {
        return controls.ContainsKey(name) ? controls[name] : null;
    }
}

