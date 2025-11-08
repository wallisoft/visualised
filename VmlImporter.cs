using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace VB;

/// <summary>
/// Imports VML files into the designer database
/// Bridges: VML → ParserManager → SQL → designer.db
/// </summary>
public class VmlImporter
{
    private static string GetDesignerDbPath()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Avalised"
        );
        
        Directory.CreateDirectory(configDir);
        return Path.Combine(configDir, "designer.db");
    }
    
    /// <summary>
    /// Imports a VML file into the designer database
    /// </summary>
    public static (bool success, string message) ImportVml(string vmlPath)
    {
        Console.WriteLine($"[VML IMPORT] Starting import: {vmlPath}");
        
        // Validate VML file exists
        if (!File.Exists(vmlPath))
        {
            var error = $"VML file not found: {vmlPath}";
            Console.WriteLine($"[VML IMPORT ERROR] {error}");
            return (false, error);
        }
        
        // Step 1: Convert VML → SQL using ParserManager
        Console.WriteLine("[VML IMPORT] Step 1: Converting VML → SQL");
        var (converted, sqlPathOrError) = ParserManager.VmlToSql(vmlPath);
        
        if (!converted)
        {
            var error = $"Failed to convert VML: {sqlPathOrError}";
            Console.WriteLine($"[VML IMPORT ERROR] {error}");
            return (false, error);
        }
        
        var sqlPath = sqlPathOrError;
        Console.WriteLine($"[VML IMPORT] ✓ SQL generated: {sqlPath}");
        
        // Step 2: Clear existing designer database
        Console.WriteLine("[VML IMPORT] Step 2: Clearing designer database");
        var clearResult = ClearDesignerDb();
        if (!clearResult.success)
        {
            return clearResult;
        }
        
        // Step 3: Execute SQL against designer.db
        Console.WriteLine("[VML IMPORT] Step 3: Importing SQL into designer.db");
        var importResult = ExecuteSqlFile(sqlPath);
        
        if (!importResult.success)
        {
            return importResult;
        }
        
        // Step 4: Verify import
        Console.WriteLine("[VML IMPORT] Step 4: Verifying import");
        var (verifySuccess, verifyMessage, controlCount) = VerifyImport();
        
        if (!verifySuccess)
        {
            return (false, verifyMessage);
        }
        
        var successMsg = $"Successfully imported {vmlPath} ({controlCount} controls)";
        Console.WriteLine($"[VML IMPORT] ✓ {successMsg}");
        
        return (true, successMsg);
    }
    
    /// <summary>
    /// Clears all controls from designer database
    /// </summary>
    private static (bool success, string message) ClearDesignerDb()
    {
        try
        {
            var dbPath = GetDesignerDbPath();
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            
            // Clear in correct order (foreign keys)
            using (var cmd = new SqliteCommand("DELETE FROM ui_attached_properties", connection))
                cmd.ExecuteNonQuery();
            
            using (var cmd = new SqliteCommand("DELETE FROM ui_properties", connection))
                cmd.ExecuteNonQuery();
            
            using (var cmd = new SqliteCommand("DELETE FROM ui_tree", connection))
                cmd.ExecuteNonQuery();
            
            Console.WriteLine("[VML IMPORT] ✓ Database cleared");
            return (true, "Database cleared");
        }
        catch (Exception ex)
        {
            var error = $"Failed to clear database: {ex.Message}";
            Console.WriteLine($"[VML IMPORT ERROR] {error}");
            return (false, error);
        }
    }
    
    /// <summary>
    /// Executes SQL file against designer database
    /// </summary>
    private static (bool success, string message) ExecuteSqlFile(string sqlPath)
    {
        try
        {
            if (!File.Exists(sqlPath))
            {
                return (false, $"SQL file not found: {sqlPath}");
            }
            
            var sql = File.ReadAllText(sqlPath);
            var dbPath = GetDesignerDbPath();
            
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            
            // SQLite requires individual statements
            var statements = sql.Split(';', StringSplitOptions.RemoveEmptyEntries);
            int executedCount = 0;
            
            foreach (var statement in statements)
            {
                var trimmed = statement.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;
                
                using var cmd = new SqliteCommand(trimmed, connection);
                cmd.ExecuteNonQuery();
                executedCount++;
            }
            
            Console.WriteLine($"[VML IMPORT] ✓ Executed {executedCount} SQL statements");
            return (true, $"Executed {executedCount} statements");
        }
        catch (Exception ex)
        {
            var error = $"Failed to execute SQL: {ex.Message}";
            Console.WriteLine($"[VML IMPORT ERROR] {error}");
            return (false, error);
        }
    }
    
    /// <summary>
    /// Verifies the import by counting controls
    /// </summary>
    private static (bool success, string message, int controlCount) VerifyImport()
    {
        try
        {
            var dbPath = GetDesignerDbPath();
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            
            using var cmd = new SqliteCommand("SELECT COUNT(*) FROM ui_tree", connection);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            
            if (count == 0)
            {
                return (false, "No controls imported", 0);
            }
            
            Console.WriteLine($"[VML IMPORT] ✓ Verified: {count} controls in database");
            return (true, $"{count} controls imported", count);
        }
        catch (Exception ex)
        {
            var error = $"Failed to verify import: {ex.Message}";
            Console.WriteLine($"[VML IMPORT ERROR] {error}");
            return (false, error, 0);
        }
    }
    
    /// <summary>
    /// Gets statistics about current designer database
    /// </summary>
    public static void PrintStats()
    {
        try
        {
            var dbPath = GetDesignerDbPath();
            Console.WriteLine($"[VML IMPORT] Designer DB: {dbPath}");
            
            if (!File.Exists(dbPath))
            {
                Console.WriteLine("[VML IMPORT] Database does not exist yet");
                return;
            }
            
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            
            using var cmd1 = new SqliteCommand("SELECT COUNT(*) FROM ui_tree", connection);
            var controlCount = Convert.ToInt32(cmd1.ExecuteScalar());
            
            using var cmd2 = new SqliteCommand("SELECT COUNT(*) FROM ui_properties", connection);
            var propCount = Convert.ToInt32(cmd2.ExecuteScalar());
            
            using var cmd3 = new SqliteCommand("SELECT COUNT(*) FROM ui_attached_properties", connection);
            var attachedCount = Convert.ToInt32(cmd3.ExecuteScalar());
            
            Console.WriteLine($"[VML IMPORT] Controls: {controlCount}");
            Console.WriteLine($"[VML IMPORT] Properties: {propCount}");
            Console.WriteLine($"[VML IMPORT] Attached Properties: {attachedCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VML IMPORT ERROR] Failed to get stats: {ex.Message}");
        }
    }
}
