using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ConfigUI
{
    public class ScriptDatabase
    {
        private string _dbPath;
        private string _connectionString;

        public ScriptDatabase(string? projectPath = null)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appDir = Path.Combine(appData, "VisualisedDesigner");
                Directory.CreateDirectory(appDir);
                _dbPath = Path.Combine(appDir, "designer.db");
            }
            else
            {
                _dbPath = projectPath;
            }
            
            _connectionString = $"Data Source={_dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createControls = @"
                CREATE TABLE IF NOT EXISTS controls (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    type TEXT NOT NULL,
                    name TEXT NOT NULL,
                    parent_id INTEGER,
                    x REAL DEFAULT 0,
                    y REAL DEFAULT 0,
                    width REAL DEFAULT 100,
                    height REAL DEFAULT 30,
                    caption TEXT,
                    text TEXT,
                    visible INTEGER DEFAULT 1,
                    enabled INTEGER DEFAULT 1,
                    FOREIGN KEY (parent_id) REFERENCES controls(id)
                );";

            var createProperties = @"
                CREATE TABLE IF NOT EXISTS properties (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    control_id INTEGER NOT NULL,
                    property_name TEXT NOT NULL,
                    property_value TEXT,
                    FOREIGN KEY (control_id) REFERENCES controls(id),
                    UNIQUE(control_id, property_name)
                );";

            var createScripts = @"
                CREATE TABLE IF NOT EXISTS scripts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    control_id INTEGER NOT NULL,
                    event_name TEXT NOT NULL,
                    script_text TEXT,
                    script_language TEXT DEFAULT 'bash',
                    FOREIGN KEY (control_id) REFERENCES controls(id),
                    UNIQUE(control_id, event_name)
                );";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = createControls;
            cmd.ExecuteNonQuery();
            
            cmd.CommandText = createProperties;
            cmd.ExecuteNonQuery();
            
            cmd.CommandText = createScripts;
            cmd.ExecuteNonQuery();

            Console.WriteLine($"✅ Database initialized at: {_dbPath}");
        }

        // ========== GENERIC SQL EXECUTION METHODS ==========
        
        public void ExecuteSql(string sql)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public object? ExecuteScalarSql(string sql)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
        }

        // ========== CONTROL METHODS ==========

        public int SaveControl(string type, string name, int? parentId, double x, double y, 
            double width, double height, string? caption, string? text, bool visible, bool enabled)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = @"
                INSERT INTO controls (type, name, parent_id, x, y, width, height, caption, text, visible, enabled)
                VALUES (@type, @name, @parentId, @x, @y, @width, @height, @caption, @text, @visible, @enabled);
                SELECT last_insert_rowid();";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@parentId", (object?)parentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@x", x);
            cmd.Parameters.AddWithValue("@y", y);
            cmd.Parameters.AddWithValue("@width", width);
            cmd.Parameters.AddWithValue("@height", height);
            cmd.Parameters.AddWithValue("@caption", (object?)caption ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@text", (object?)text ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@visible", visible ? 1 : 0);
            cmd.Parameters.AddWithValue("@enabled", enabled ? 1 : 0);

            var id = Convert.ToInt32(cmd.ExecuteScalar());
            Console.WriteLine($"✅ Saved control: {name} (ID: {id})");
            return id;
        }

        public void UpdateControl(int id, double x, double y, double width, double height, 
            string? caption, string? text, bool visible, bool enabled)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE controls 
                SET x = @x, y = @y, width = @width, height = @height, 
                    caption = @caption, text = @text, visible = @visible, enabled = @enabled
                WHERE id = @id;";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@x", x);
            cmd.Parameters.AddWithValue("@y", y);
            cmd.Parameters.AddWithValue("@width", width);
            cmd.Parameters.AddWithValue("@height", height);
            cmd.Parameters.AddWithValue("@caption", (object?)caption ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@text", (object?)text ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@visible", visible ? 1 : 0);
            cmd.Parameters.AddWithValue("@enabled", enabled ? 1 : 0);
            
            cmd.ExecuteNonQuery();
        }

        public void DeleteControl(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Delete scripts first (foreign key)
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM scripts WHERE control_id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            // Delete properties
            cmd.CommandText = "DELETE FROM properties WHERE control_id = @id;";
            cmd.ExecuteNonQuery();

            // Delete control
            cmd.CommandText = "DELETE FROM controls WHERE id = @id;";
            cmd.ExecuteNonQuery();

            Console.WriteLine($"✅ Deleted control ID: {id}");
        }

        public List<Dictionary<string, object>> GetAllControls()
        {
            var controls = new List<Dictionary<string, object>>();
            
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = "SELECT * FROM controls ORDER BY id;";
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var control = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    control[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                controls.Add(control);
            }
            
            return controls;
        }

        // ========== SCRIPT METHODS ==========

        public void SaveScript(int controlId, string eventName, string scriptText, string language = "bash")
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = @"
                INSERT INTO scripts (control_id, event_name, script_text, script_language)
                VALUES (@controlId, @eventName, @scriptText, @language)
                ON CONFLICT(control_id, event_name) 
                DO UPDATE SET script_text = @scriptText, script_language = @language;";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@controlId", controlId);
            cmd.Parameters.AddWithValue("@eventName", eventName);
            cmd.Parameters.AddWithValue("@scriptText", scriptText);
            cmd.Parameters.AddWithValue("@language", language);
            
            cmd.ExecuteNonQuery();
            Console.WriteLine($"✅ Saved script: {eventName} for control {controlId}");
        }

        public string? GetScript(int controlId, string eventName)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = "SELECT script_text FROM scripts WHERE control_id = @controlId AND event_name = @eventName;";
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@controlId", controlId);
            cmd.Parameters.AddWithValue("@eventName", eventName);
            
            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }

        public Dictionary<string, string> GetAllScriptsForControl(int controlId)
        {
            var scripts = new Dictionary<string, string>();
            
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = "SELECT event_name, script_text FROM scripts WHERE control_id = @controlId;";
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@controlId", controlId);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var eventName = reader.GetString(0);
                var scriptText = reader.IsDBNull(1) ? "" : reader.GetString(1);
                scripts[eventName] = scriptText;
            }
            
            return scripts;
        }

        public void DeleteScript(int controlId, string eventName)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = "DELETE FROM scripts WHERE control_id = @controlId AND event_name = @eventName;";
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@controlId", controlId);
            cmd.Parameters.AddWithValue("@eventName", eventName);
            
            cmd.ExecuteNonQuery();
            Console.WriteLine($"✅ Deleted script: {eventName} for control {controlId}");
        }

        // ========== PROPERTY METHODS ==========

        public void SaveProperty(int controlId, string propertyName, string propertyValue)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = @"
                INSERT INTO properties (control_id, property_name, property_value)
                VALUES (@controlId, @propertyName, @propertyValue)
                ON CONFLICT(control_id, property_name) 
                DO UPDATE SET property_value = @propertyValue;";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@controlId", controlId);
            cmd.Parameters.AddWithValue("@propertyName", propertyName);
            cmd.Parameters.AddWithValue("@propertyValue", propertyValue);
            
            cmd.ExecuteNonQuery();
        }

        public Dictionary<string, string> GetPropertiesForControl(int controlId)
        {
            var properties = new Dictionary<string, string>();
            
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = "SELECT property_name, property_value FROM properties WHERE control_id = @controlId;";
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@controlId", controlId);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var propName = reader.GetString(0);
                var propValue = reader.IsDBNull(1) ? "" : reader.GetString(1);
                properties[propName] = propValue;
            }
            
            return properties;
        }

        public void ClearAllData()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM scripts;";
            cmd.ExecuteNonQuery();
            
            cmd.CommandText = "DELETE FROM properties;";
            cmd.ExecuteNonQuery();
            
            cmd.CommandText = "DELETE FROM controls;";
            cmd.ExecuteNonQuery();

            Console.WriteLine("✅ Cleared all database data");
        }
    }
}