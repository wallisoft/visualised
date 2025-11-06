#!/bin/bash
# Visualised Markup - Stub Extractor
# Extracts and builds from database

set -e

DB="vb-source.db"

if [ ! -f "$DB" ]; then
    echo "❌ Database not found: $DB"
    exit 1
fi

echo "🌳 Visualised Markup - Building from Database"
echo "════════════════════════════════════════"
echo ""

# Extract all files
echo "📤 Extracting source files..."
sqlite3 "$DB" "SELECT filename FROM source_files" | while read -r file; do
    echo "  → $file"
    sqlite3 "$DB" "SELECT content FROM source_files WHERE filename='$file'" > "$file"
done

echo ""
echo "📤 Extracting VML files..."
sqlite3 "$DB" "SELECT filename FROM vml_files" | while read -r file; do
    echo "  → $file"
    sqlite3 "$DB" "SELECT content FROM vml_files WHERE filename='$file'" > "$file"
done

echo ""
echo "📤 Extracting documentation..."
sqlite3 "$DB" "SELECT filename FROM documentation" | while read -r file; do
    echo "  → $file"
    sqlite3 "$DB" "SELECT content FROM documentation WHERE filename='$file'" > "$file"
done

echo ""
echo "📤 Extracting build scripts..."
mkdir -p scripts
sqlite3 "$DB" "SELECT name FROM build_scripts" | while read -r file; do
    echo "  → $file"
    sqlite3 "$DB" "SELECT content FROM build_scripts WHERE name='$file'" > "$file"
    chmod +x "$file"
done

echo ""
echo "🔨 Building..."
dotnet build

echo ""
echo "════════════════════════════════════════"
echo "✓ Build complete from database!"
echo "════════════════════════════════════════"
echo ""
echo "Run: ./bin/Debug/net9.0/VB"
