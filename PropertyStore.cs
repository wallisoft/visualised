using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;

namespace VB;

public static class PropertyStore
{
    private static string GetDbPath()
    {
        // Check if running from system install location
        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var exeDir = Path.GetDirectoryName(exePath);
        
        bool isSystemInstall = exeDir?.StartsWith("/usr/") == true || 
                               exeDir?.StartsWith("/opt/") == true;
        
        if (isSystemInstall)
        {
            // System-wide install: /var/lib/visualised/
            var dbPath = "/var/lib/visualised/visualised.db";
            var dbDir = Path.GetDirectoryName(dbPath);
            
            // Try to create system directory (needs sudo/root)
            if (!Directory.Exists(dbDir))
            {
                try
                {
                    Directory.CreateDirectory(dbDir!);
                }
                catch
                {
                    // Fallback to user dir if cant write to /var/lib
                    goto UserInstall;
                }
            }
            return dbPath;
        }
        
        UserInstall:
        // User install or fallback: ~/.visualised/
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".visualised",
            "visualised.db"
        );
    }
    
    private static string DbPath => GetDbPath();
    
    private static SqliteConnection? connection;
    
    public static void Initialize()
    {
        try
        {
            // Create directory if needed
            var dbDir = Path.GetDirectoryName(DbPath);
            if (!Directory.Exists(dbDir))
                Directory.CreateDirectory(dbDir!);
                
            connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();
            
            // Create properties table
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS properties (
                    control_name TEXT NOT NULL,
                    property_name TEXT NOT NULL,
                    property_value TEXT,
                    PRIMARY KEY (control_name, property_name)
                )";
            cmd.ExecuteNonQuery();
            
            Console.WriteLine($"[PROPERTY STORE] Initialized at {DbPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROPERTY STORE] Error: {ex.Message}");
        }
    }
    
    public static void Set(string controlName, string propertyName, string? value)
    {
        if (connection == null) Initialize();
        
        try
        {
            var cmd = connection!.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO properties (control_name, property_name, property_value)
                VALUES (@name, @prop, @value)";
            cmd.Parameters.AddWithValue("@name", controlName);
            cmd.Parameters.AddWithValue("@prop", propertyName);
            cmd.Parameters.AddWithValue("@value", value ?? string.Empty);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROPERTY STORE] Set error: {ex.Message}");
        }
    }
    
    public static string? Get(string controlName, string propertyName)
    {
        if (connection == null) Initialize();
        
        try
        {
            var cmd = connection!.CreateCommand();
            cmd.CommandText = @"
                SELECT property_value FROM properties 
                WHERE control_name = @name AND property_name = @prop";
            cmd.Parameters.AddWithValue("@name", controlName);
            cmd.Parameters.AddWithValue("@prop", propertyName);
            
            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROPERTY STORE] Get error: {ex.Message}");
            return null;
        }
    }
    
    public static void SyncControl(Avalonia.Controls.Control control)
    {
        if (control.Name == null) return;
        
        var props = control.GetType().GetProperties();
        foreach (var prop in props)
        {
            try
            {
                var value = prop.GetValue(control);
                if (value != null)
                    Set(control.Name, prop.Name, value.ToString());
            }
            catch
            {
                // Skip properties that cant be read
            }
        }
    }
    
    private static Dictionary<string, object?> GetControlProperties(string controlName)
    {
        var props = new Dictionary<string, object?>();
        
        try
        {
            var cmd = connection!.CreateCommand();
            cmd.CommandText = "SELECT property_name, property_value FROM properties WHERE control_name = @name";
            cmd.Parameters.AddWithValue("@name", controlName);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                props[reader.GetString(0)] = reader.GetString(1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROPERTY STORE] Query error: {ex.Message}");
        }
        
        return props;
    }
    
    public static void Close()
    {
        connection?.Close();
    }
}
