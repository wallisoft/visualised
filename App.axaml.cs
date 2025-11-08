using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Linq;

namespace VB;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = Program.CommandLineArgs ?? Array.Empty<string>();
            
            // Check for runtime mode: --app path.vml or --runtime path.vml
            var runtimeMode = args.Contains("--app") || args.Contains("--runtime");
            
            if (runtimeMode && args.Length > 1)
            {
                // Runtime mode - load VML app directly
                var vmlPath = args[1];
                
                Console.WriteLine($"📂 Runtime Mode: Loading {vmlPath}");
                
                var appWindow = VmlWindowLoader.LoadWindow(vmlPath);
                
                if (appWindow != null)
                {
                    Console.WriteLine($"✅ App loaded: {appWindow.Title}");
                    desktop.MainWindow = appWindow;
                }
                else
                {
                    Console.WriteLine("❌ Failed to load VML app!");
                    Console.WriteLine("Falling back to designer mode...");
                    
                    // Fallback to designer
                    var mainWindow = new MainWindow();
                    DesignerWindow.LoadAndApply(mainWindow, "vml/designer.vml");
                    desktop.MainWindow = mainWindow;
                }
            }
            else
            {
                // IDE mode - load designer
                Console.WriteLine("🎨 IDE Mode: Loading designer");
                
                var mainWindow = new MainWindow();
                DesignerWindow.LoadAndApply(mainWindow, "vml/designer.vml");
                desktop.MainWindow = mainWindow;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}

