using System;
using Avalonia.Interactivity;

namespace VB;

public static class AppHandlers
{
    public static void FileExit_Click(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("[HANDLER] File → Exit clicked!");
        Environment.Exit(0);
    }
    
    public static void HelpAbout_Click(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("[HANDLER] Help → About clicked!");
        // TODO: Show About dialog
    }
}
