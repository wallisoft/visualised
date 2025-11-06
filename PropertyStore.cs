using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;

namespace VB;

public static class PropertyStore
{
    private static string DbPath => Path.Combine(
        Path.GetTempPath(), 
        "vb-runtime.db"
    );
    
    private static SqliteConnection? connection;
    
    public static void Initialize()
    {
        try
        {
            // Delete old database
            if (File.Exists(DbPath))
                File.Delete(DbPath);
            
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
        if (connection == null) return;
        
        try
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO properties (control_name, property_name, property_value)
                VALUES (@control, @property, @value)";
            cmd.Parameters.AddWithValue("@control", controlName);
            cmd.Parameters.AddWithValue("@property", propertyName);
            cmd.Parameters.AddWithValue("@value", value ?? "");
            cmd.ExecuteNonQuery();
            
            Console.WriteLine($"[PROPERTY STORE] Set {controlName}.{propertyName} = {value}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROPERTY STORE] Set error: {ex.Message}");
        }
    }
    
    public static string? Get(string controlName, string propertyName)
    {
        if (connection == null) Initialize();
        if (connection == null) return null;
        
        try
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT property_value FROM properties 
                WHERE control_name = @control AND property_name = @property";
            cmd.Parameters.AddWithValue("@control", controlName);
            cmd.Parameters.AddWithValue("@property", propertyName);
            
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
        if (string.IsNullOrEmpty(control.Name)) return;
        
        // Sync all important properties to database
        var props = control.GetType().GetProperties();
        foreach (var prop in props)
        {
            if (!prop.CanRead) continue;
            
            try
            {
                var value = prop.GetValue(control);
                if (value != null && IsSimpleType(prop.PropertyType))
                {
                    Set(control.Name, prop.Name, value.ToString());
                }
            }
            catch { }
        }
    }
    
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || 
               type == typeof(string) || 
               type == typeof(decimal) || 
               type == typeof(DateTime);
    }
    
    public static void Close()
    {
        connection?.Close();
        connection?.Dispose();
        connection = null;
    }
}

