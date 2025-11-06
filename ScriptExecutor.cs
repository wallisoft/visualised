using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VB;

public class ScriptExecutor
{
    public static void ExecuteScript(string script, Control? control = null)
    {
        if (script.StartsWith("bash:"))
        {
            ExecuteBash(script.Substring(5), control);
        }
        else if (script.StartsWith("python:"))
        {
            ExecutePython(script.Substring(7), control);
        }
        else
        {
            // Assume inline bash
            ExecuteBash(script, control);
        }
    }
    
    private static void ExecuteBash(string command, Control? control)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            if (!string.IsNullOrEmpty(output))
            {
                Console.WriteLine($"[SCRIPT] Output: {output}");
            }
            
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"[SCRIPT] Error: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SCRIPT] Failed to execute: {ex.Message}");
        }
    }
    
    private static void ExecutePython(string command, Control? control)
    {
        // Similar to bash but with python3
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            Console.WriteLine($"[SCRIPT] Output: {output}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SCRIPT] Failed: {ex.Message}");
        }
    }
}

