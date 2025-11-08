using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace VB;

/// <summary>
/// Self-replicating database manager
/// Stores all source code with 30 rolling backups
/// Can rebuild entire project from database
/// </summary>
public class DatabaseManager
{
    private readonly string dbPath;
    private readonly string connectionString;
    
    public DatabaseManager(string dbFile = "visualised.db")
    {
        dbPath = Path.Combine(Environment.CurrentDirectory, dbFile);
        connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }
    
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        // Run migration SQL
        var migrationSql = File.Exists("migrate_source_db.sql") 
            ? File.ReadAllText("migrate_source_db.sql")
            : GetEmbeddedMigration();
        
        var statements = migrationSql.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var statement in statements)
        {
            var trimmed = statement.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("--")) 
                continue;
            
            try
            {
                using var cmd = new SqliteCommand(trimmed, connection);
                cmd.ExecuteNonQuery();
            }
            catch (SqliteException ex)
            {
                // Ignore "already exists" errors during migration
                if (!ex.Message.Contains("already exists"))
                    Console.WriteLine($"[DB MIGRATION] Warning: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Saves file with automatic backup
    /// Triggers maintain 30 rolling backups
    /// </summary>
    public void SaveFile(string filename, string content, string fileType, string comment = "")
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Check if file exists
        var checkSql = "SELECT version FROM project_files WHERE filename = @filename";
        using var checkCmd = new SqliteCommand(checkSql, connection);
        checkCmd.Parameters.AddWithValue("@filename", filename);
        var existingVersion = checkCmd.ExecuteScalar();
        
        if (existingVersion != null)
        {
            // Update existing (trigger will auto-backup)
            var updateSql = @"
                UPDATE project_files 
                SET content = @content, 
                    updated_at = @timestamp, 
                    version = version + 1
                WHERE filename = @filename
            ";
            
            using var updateCmd = new SqliteCommand(updateSql, connection);
            updateCmd.Parameters.AddWithValue("@content", content);
            updateCmd.Parameters.AddWithValue("@timestamp", timestamp);
            updateCmd.Parameters.AddWithValue("@filename", filename);
            updateCmd.ExecuteNonQuery();
            
            Console.WriteLine($"[DB] ✓ Updated {filename} v{Convert.ToInt32(existingVersion) + 1}");
        }
        else
        {
            // Insert new
            var insertSql = @"
                INSERT INTO project_files (filename, content, file_type, created_at, updated_at, version)
                VALUES (@filename, @content, @fileType, @timestamp, @timestamp, 1)
            ";
            
            using var insertCmd = new SqliteCommand(insertSql, connection);
            insertCmd.Parameters.AddWithValue("@filename", filename);
            insertCmd.Parameters.AddWithValue("@content", content);
            insertCmd.Parameters.AddWithValue("@fileType", fileType);
            insertCmd.Parameters.AddWithValue("@timestamp", timestamp);
            insertCmd.ExecuteNonQuery();
            
            Console.WriteLine($"[DB] ✓ Created {filename} v1");
        }
    }
    
    /// <summary>
    /// Load current version of file
    /// </summary>
    public string? LoadFile(string filename)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var sql = "SELECT content FROM project_files WHERE filename = @filename";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@filename", filename);
        
        return command.ExecuteScalar()?.ToString();
    }
    
    /// <summary>
    /// Load specific backup version
    /// </summary>
    public string? LoadBackup(string filename, int version)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var sql = "SELECT content FROM source_backups WHERE filename = @filename AND version = @version";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@filename", filename);
        command.Parameters.AddWithValue("@version", version);
        
        return command.ExecuteScalar()?.ToString();
    }
    
    /// <summary>
    /// Restore from backup
    /// </summary>
    public bool RestoreFromBackup(string filename, int version)
    {
        var backup = LoadBackup(filename, version);
        if (backup == null) return false;
        
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        // Get file type from backups
        var typeSql = "SELECT file_type FROM source_backups WHERE filename = @filename AND version = @version";
        using var typeCmd = new SqliteCommand(typeSql, connection);
        typeCmd.Parameters.AddWithValue("@filename", filename);
        typeCmd.Parameters.AddWithValue("@version", version);
        var fileType = typeCmd.ExecuteScalar()?.ToString() ?? "cs";
        
        SaveFile(filename, backup, fileType, $"Restored from v{version}");
        Console.WriteLine($"[DB] ✓ Restored {filename} from v{version}");
        return true;
    }
    
    /// <summary>
    /// Import all source files from filesystem into database
    /// </summary>
    public int ImportSource(string sourceDir = ".")
    {
        var extensions = new[] { "*.cs", "*.vml", "*.sql", "*.sh", "*.md", "*.csproj" };
        var imported = 0;
        
        Console.WriteLine($"[DB IMPORT] Scanning {sourceDir}...");
        
        foreach (var ext in extensions)
        {
            var files = Directory.GetFiles(sourceDir, ext, SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                
                // Skip build artifacts and temp files
                if (filename.Contains(".dll") || filename.Contains(".pdb") || 
                    filename.Contains(".obj") || filename.StartsWith("._"))
                    continue;
                
                var content = File.ReadAllText(file);
                var fileType = Path.GetExtension(filename).TrimStart('.');
                
                SaveFile(filename, content, fileType, "Imported from filesystem");
                imported++;
            }
        }
        
        Console.WriteLine($"[DB IMPORT] ✓ Imported {imported} files");
        return imported;
    }
    
    /// <summary>
    /// Export all source files from database to filesystem
    /// REBUILDS THE PROJECT FROM DATABASE!
    /// </summary>
    public int ExportSource(string targetDir = ".")
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var sql = "SELECT filename, content FROM project_files ORDER BY filename";
        using var command = new SqliteCommand(sql, connection);
        using var reader = command.ExecuteReader();
        
        var exported = 0;
        Console.WriteLine($"[DB EXPORT] Rebuilding source to {targetDir}...");
        
        while (reader.Read())
        {
            var filename = reader["filename"].ToString();
            var content = reader["content"].ToString();
            
            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(content))
                continue;
            
            var targetPath = Path.Combine(targetDir, filename);
            File.WriteAllText(targetPath, content);
            Console.WriteLine($"[DB EXPORT] ✓ {filename}");
            exported++;
        }
        
        Console.WriteLine($"[DB EXPORT] ✓ Exported {exported} files");
        return exported;
    }
    
    /// <summary>
    /// Delete file and backup history
    /// </summary>
    public void DeleteFile(string filename, bool keepBackups = true)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        // Delete triggers will auto-backup
        var sql = "DELETE FROM project_files WHERE filename = @filename";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@filename", filename);
        command.ExecuteNonQuery();
        
        if (!keepBackups)
        {
            var deleteBkSql = "DELETE FROM source_backups WHERE filename = @filename";
            using var bkCmd = new SqliteCommand(deleteBkSql, connection);
            bkCmd.Parameters.AddWithValue("@filename", filename);
            bkCmd.ExecuteNonQuery();
        }
        
        Console.WriteLine($"[DB] ✓ Deleted {filename}");
    }
    
    /// <summary>
    /// List all source files
    /// </summary>
    public void ListFiles()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var sql = @"
            SELECT filename, file_type, version, updated_at, 
                   length(content) as size,
                   (SELECT COUNT(*) FROM source_backups WHERE filename = pf.filename) as backups
            FROM project_files pf
            ORDER BY file_type, filename
        ";
        
        using var command = new SqliteCommand(sql, connection);
        using var reader = command.ExecuteReader();
        
        Console.WriteLine("\n[DB] Current Source Files:");
        Console.WriteLine("─────────────────────────────────────────────────────────");
        
        while (reader.Read())
        {
            var filename = reader["filename"].ToString();
            var fileType = reader["file_type"].ToString();
            var version = reader["version"];
            var size = Convert.ToInt32(reader["size"]);
            var backups = reader["backups"];
            var updated = reader["updated_at"];
            
            Console.WriteLine($"  {filename,-30} {fileType,5} v{version,-3} {size,6}b ({backups} backups) {updated}");
        }
    }
    
    /// <summary>
    /// Show backup history for file
    /// </summary>
    public void ShowHistory(string filename)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var sql = @"
            SELECT version, backed_up_at, comment, file_size
            FROM source_backups
            WHERE filename = @filename
            ORDER BY version DESC
        ";
        
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@filename", filename);
        using var reader = command.ExecuteReader();
        
        Console.WriteLine($"\n[DB] Backup History: {filename}");
        Console.WriteLine("─────────────────────────────────────────────────────────");
        
        while (reader.Read())
        {
            var version = reader["version"];
            var when = reader["backed_up_at"];
            var comment = reader["comment"];
            var size = reader["file_size"];
            
            Console.WriteLine($"  v{version,-4} {when,-20} {size,6}b  {comment}");
        }
    }
    
    /// <summary>
    /// Clean production database (remove all backups)
    /// </summary>
    public void CleanProduction()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var countSql = "SELECT COUNT(*) FROM source_backups";
        using var countCmd = new SqliteCommand(countSql, connection);
        var backupCount = Convert.ToInt32(countCmd.ExecuteScalar());
        
        var deleteSql = "DELETE FROM source_backups";
        using var deleteCmd = new SqliteCommand(deleteSql, connection);
        deleteCmd.ExecuteNonQuery();
        
        Console.WriteLine($"[DB] ✓ Production clean: Removed {backupCount} backups");
    }
    
    /// <summary>
    /// Database statistics
    /// </summary>
    public void PrintStats()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var sql = @"
            SELECT 
                (SELECT COUNT(*) FROM project_files) as files,
                (SELECT COUNT(*) FROM source_backups) as backups,
                (SELECT SUM(length(content)) FROM project_files) as total_size,
                (SELECT COUNT(DISTINCT filename) FROM source_backups) as files_with_backups
        ";
        
        using var command = new SqliteCommand(sql, connection);
        using var reader = command.ExecuteReader();
        
        if (reader.Read())
        {
            Console.WriteLine("\n[DB] Statistics:");
            Console.WriteLine($"  Database: {dbPath}");
            Console.WriteLine($"  Current Files: {reader["files"]}");
            Console.WriteLine($"  Total Backups: {reader["backups"]}");
            Console.WriteLine($"  Files with History: {reader["files_with_backups"]}");
            Console.WriteLine($"  Total Size: {Convert.ToInt64(reader["total_size"]):N0} bytes");
        }
    }
    
    private string GetEmbeddedMigration()
    {
        // Fallback if migrate_source_db.sql not found
        return @"
            CREATE TABLE IF NOT EXISTS project_files (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                filename TEXT NOT NULL UNIQUE,
                content TEXT NOT NULL,
                file_type TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                version INTEGER DEFAULT 1
            );
            
            CREATE TABLE IF NOT EXISTS source_backups (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                filename TEXT NOT NULL,
                content TEXT NOT NULL,
                file_type TEXT NOT NULL,
                version INTEGER NOT NULL,
                backed_up_at TEXT NOT NULL,
                comment TEXT,
                file_size INTEGER NOT NULL
            );
        ";
    }
    
    public string GetDbPath() => dbPath;
}
