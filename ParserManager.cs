using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace VB;

public class ParserManager
{
    private static Dictionary<string, string> defaultParsers = new()
    {
        { "vml", "vml-sql-parser" },
        { "sql", "vml-sql-parser" },
        { "yaml", "yaml-parser" }
    };
    
    public static string GetParserPath(string format)
    {
        if (defaultParsers.ContainsKey(format.ToLower()))
        {
            var parserName = defaultParsers[format.ToLower()];
            var parserPath = Path.Combine("parsers", parserName, "VmlParser");
            
            if (File.Exists(parserPath))
                return parserPath;
            
            // Try with .exe extension (Windows)
            if (File.Exists(parserPath + ".exe"))
                return parserPath + ".exe";
        }
        
        return null;
    }
    
    public static (bool success, string output) Convert(
        string inputFile, 
        string fromFormat, 
        string toFormat,
        string outputFile = null)
    {
        Console.WriteLine($"[PARSER] Converting {fromFormat} → {toFormat}");
        Console.WriteLine($"[PARSER] Input: {inputFile}");
        
        var parserPath = GetParserPath(fromFormat);
        if (parserPath == null)
        {
            Console.WriteLine($"[PARSER ERROR] No parser found for format: {fromFormat}");
            return (false, $"No parser for {fromFormat}");
        }
        
        Console.WriteLine($"[PARSER] Using: {parserPath}");
        
        // Generate output filename if not provided
        if (string.IsNullOrEmpty(outputFile))
        {
            var dir = Path.GetDirectoryName(inputFile);
            var filename = Path.GetFileNameWithoutExtension(inputFile);
            outputFile = Path.Combine(dir, $"{filename}.{toFormat}");
        }
        
        Console.WriteLine($"[PARSER] Output: {outputFile}");
        
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = parserPath,
                Arguments = $"--from {fromFormat} --to {toFormat} --input \"{inputFile}\" --output \"{outputFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            var process = Process.Start(startInfo);
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            Console.WriteLine($"[PARSER OUTPUT] {output}");
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"[PARSER ERROR] {error}");
            }
            
            if (process.ExitCode == 0)
            {
                Console.WriteLine($"[PARSER] ✓ Success!");
                return (true, outputFile);
            }
            else
            {
                Console.WriteLine($"[PARSER] ✗ Failed with exit code {process.ExitCode}");
                return (false, error);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PARSER EXCEPTION] {ex.Message}");
            return (false, ex.Message);
        }
    }
    
    public static (bool success, string vmlPath) SqlToVml(string sqlFile)
    {
        return Convert(sqlFile, "sql", "vml");
    }
    
    public static (bool success, string sqlPath) VmlToSql(string vmlFile)
    {
        return Convert(vmlFile, "vml", "sql");
    }
}
