using Avalonia;
using System;

namespace VB;

class Program
{
    public static string[]? CommandLineArgs { get; private set; }
    
    [STAThread]
    public static void Main(string[] args)
    {
        CommandLineArgs = args;
        
        // Check for runtime mode
        if (args.Length > 0)
        {
            Console.WriteLine("═══════════════════════════════════");
            Console.WriteLine("🚀 VB Runtime v1.0");
            Console.WriteLine("═══════════════════════════════════");
            
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine($"  Arg[{i}]: {args[i]}");
            }
            Console.WriteLine();
        }
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();

}
