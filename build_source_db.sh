#!/bin/bash
set -e

echo "Building source database with all files..."

# Remove old databases
rm -f visualised.db seed.db

# Create database with schema
sqlite3 visualised.db << EOF
-- Runtime properties
CREATE TABLE properties (
    control_name TEXT NOT NULL,
    property_name TEXT NOT NULL,
    property_value TEXT,
    PRIMARY KEY (control_name, property_name)
);

-- Source files (THE ENTIRE SOURCE)
CREATE TABLE source_files (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    path TEXT NOT NULL UNIQUE,
    content BLOB NOT NULL,
    file_type TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Installed VML applications
CREATE TABLE apps (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    vml_content TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- System metadata
INSERT INTO properties VALUES ("_system", "_version", "1.0");
EOF

echo "Inserting source files..."

# Use Python for proper binary/text handling
python3 << 'ENDPYTHON'
import sqlite3
import os
import glob

conn = sqlite3.connect("visualised.db")
cursor = conn.cursor()

def insert_file(filepath, filetype):
    if os.path.isfile(filepath):
        print(f"  → {filepath}")
        with open(filepath, "r", encoding="utf-8") as f:
            content = f.read()
        cursor.execute(
            "INSERT INTO source_files (path, content, file_type) VALUES (?, ?, ?)",
            (filepath, content, filetype)
        )

# C# files
for f in glob.glob("*.cs"):
    insert_file(f, "cs")

# VML files
for f in glob.glob("vml/*.vml"):
    insert_file(f, "vml")

# Project files
for f in glob.glob("*.csproj") + glob.glob("*.axaml") + ["app.manifest"]:
    if os.path.isfile(f):
        insert_file(f, "project")

# Docs
for f in glob.glob("*.md"):
    insert_file(f, "doc")

# Shell scripts
for f in ["build_source_db.sh", "stub.sh", "create_seed_db.sh"]:
    if os.path.isfile(f):
        insert_file(f, "script")

# Gitignore
if os.path.isfile(".gitignore"):
    insert_file(".gitignore", "config")

conn.commit()
conn.close()
ENDPYTHON

# Show stats
echo ""
echo "=== Database built: visualised.db ==="
ls -lh visualised.db
echo ""
echo "Files by type:"
sqlite3 visualised.db "SELECT file_type, COUNT(*) as count FROM source_files GROUP BY file_type ORDER BY count DESC;"
echo ""
sqlite3 visualised.db "SELECT COUNT(*) || ' total files' FROM source_files;"
echo ""
echo "Database is self-contained source!"
