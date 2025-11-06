using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace VB;

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
        
        var createTable = @"
            CREATE TABLE IF NOT EXISTS project_files (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                filename TEXT NOT NULL UNIQUE,
                content TEXT NOT NULL,
                file_type TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                version INTEGER DEFAULT 1
            );
        ";
        
        using var command = new SqliteCommand(createTable, connection);
        command.ExecuteNonQuery();
    }
    
    public void SaveFile(string filename, string content, string fileType)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Try update first
        var updateSql = @"
            UPDATE project_files 
            SET content = @content, updated_at = @timestamp, version = version + 1
            WHERE filename = @filename
        ";
        
        using var updateCmd = new SqliteCommand(updateSql, connection);
        updateCmd.Parameters.AddWithValue("@content", content);
        updateCmd.Parameters.AddWithValue("@timestamp", timestamp);
        updateCmd.Parameters.AddWithValue("@filename", filename);
        
        var rowsAffected = updateCmd.ExecuteNonQuery();
        
        if (rowsAffected == 0)
        {
            // Insert if not exists
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
        }
    }
    
    public string? LoadFile(string filename)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var sql = "SELECT content FROM project_files WHERE filename = @filename";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@filename", filename);
        
        var result = command.ExecuteScalar();
        return result?.ToString();
    }
    
    public void DeleteFile(string filename)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var sql = "DELETE FROM project_files WHERE filename = @filename";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@filename", filename);
        command.ExecuteNonQuery();
    }
    
    public void ListFiles()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        var sql = "SELECT filename, file_type, version, updated_at FROM project_files ORDER BY updated_at DESC";
        using var command = new SqliteCommand(sql, connection);
        using var reader = command.ExecuteReader();
        
        Console.WriteLine("\n[DB] Project Files:");
        while (reader.Read())
        {
            Console.WriteLine($"  {reader["filename"]} ({reader["file_type"]}) v{reader["version"]} - {reader["updated_at"]}");
        }
    }
    
    public string GetDbPath() => dbPath;
}

