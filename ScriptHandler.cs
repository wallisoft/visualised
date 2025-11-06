using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VB;

public static class ScriptHandler
{
    private static readonly Dictionary<string, string> InterpreterCommands = new()
    {
        { "bash", "/bin/bash" },
        { "python", "/usr/bin/python3" },
        { "node", "/usr/bin/node" },
        { "ruby", "/usr/bin/ruby" },
        { "perl", "/usr/bin/perl" }
    };
    
    public static void Execute(string scriptCode, string interpreter, Dictionary<string, string>? args = null)
    {
        // Extract clean interpreter name (remove UI text like "(JavaScript)")
        var cleanInterp = interpreter.Split(' ')[0].ToLower();
        
        if (!InterpreterCommands.TryGetValue(cleanInterp, out var interpreterPath))
        {
            Console.WriteLine($"[SCRIPT] Unknown interpreter: {interpreter}");
            return;
        }
        
        // Check if interpreter exists
        if (!File.Exists(interpreterPath))
        {
            Console.WriteLine($"[SCRIPT] Interpreter not found: {interpreterPath}");
            Console.WriteLine($"[SCRIPT] Install {cleanInterp} to use this language");
            return;
        }
        
        try
        {
            // Create temp script file
            var tempFile = Path.Combine(Path.GetTempPath(), $"vb-script-{Guid.NewGuid()}.{GetExtension(cleanInterp)}");
            File.WriteAllText(tempFile, scriptCode);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = interpreterPath,
                ArgumentList = { tempFile },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            
            // Pass context as environment variables
            if (args != null)
            {
                foreach (var kvp in args)
                {
                    startInfo.Environment[$"VML_{kvp.Key.ToUpper()}"] = kvp.Value;
                }
            }
            
            Console.WriteLine($"[SCRIPT] Executing with {cleanInterp}...");
            
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(output))
                    Console.WriteLine($"[SCRIPT OUTPUT]\n{output}");
                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine($"[SCRIPT ERROR]\n{error}");
                
                Console.WriteLine($"[SCRIPT] Exit code: {process.ExitCode}");
            }
            
            // Cleanup
            try { File.Delete(tempFile); } catch { }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SCRIPT] Execution error: {ex.Message}");
        }
    }
    
    private static string GetExtension(string interpreter)
    {
        return interpreter switch
        {
            "bash" => "sh",
            "python" => "py",
            "node" => "js",
            "ruby" => "rb",
            "perl" => "pl",
            _ => "txt"
        };
    }
    
    public static bool IsInterpreterAvailable(string interpreter)
    {
        var cleanInterp = interpreter.Split(' ')[0].ToLower();
        if (!InterpreterCommands.TryGetValue(cleanInterp, out var path))
            return false;
        return File.Exists(path);
    }
}

