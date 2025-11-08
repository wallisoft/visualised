#!/bin/bash
set -e

DB_FILE="visualised.db"
INSTALL_DIR="${INSTALL_DIR:-$HOME/.local}"
SYSTEM_INSTALL=false

echo "╔════════════════════════════════════════╗"
echo "║  Visualised Markup Installer v1.0     ║"
echo "╚════════════════════════════════════════╝"
echo ""

# Check for database
if [ ! -f "$DB_FILE" ]; then
    echo "✗ visualised.db not found!"
    echo ""
    echo "Download it first:"
    echo "  curl -O https://raw.githubusercontent.com/wallisoft/visualised/main/visualised.db"
    exit 1
fi

# Check for sudo/root for system install
if [ "$EUID" -eq 0 ] || [ "$1" = "--system" ]; then
    SYSTEM_INSTALL=true
    INSTALL_DIR="/usr/local"
    DB_DIR="/var/lib/visualised"
    echo "→ System-wide install to $INSTALL_DIR"
else
    DB_DIR="$HOME/.config/visualised"
    echo "→ User install to $INSTALL_DIR"
fi

# Check dependencies
echo ""
echo "Checking dependencies..."
command -v dotnet >/dev/null 2>&1 || { echo "✗ .NET 9 SDK required. Install from: https://dot.net"; exit 1; }
command -v sqlite3 >/dev/null 2>&1 || { echo "✗ sqlite3 required"; exit 1; }
echo "✓ Dependencies OK"

# Extract source from database
echo ""
echo "Extracting source from database..."
rm -rf /tmp/vb-build
mkdir -p /tmp/vb-build
cd /tmp/vb-build

# Extract all files using Python (handles binary properly)
python3 << 'ENDPYTHON'
import sqlite3
import os

conn = sqlite3.connect("${DB_FILE}")
cursor = conn.cursor()

cursor.execute("SELECT path, content FROM source_files")
for row in cursor.fetchall():
    filepath, content = row
    dirpath = os.path.dirname(filepath)
    if dirpath:
        os.makedirs(dirpath, exist_ok=True)
    
    with open(filepath, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"  → {filepath}")

conn.close()
ENDPYTHON

echo "✓ Extracted $(find . -type f | wc -l) files"

# Build
echo ""
echo "Building VB..."
dotnet build -c Release >/dev/null 2>&1 || { echo "✗ Build failed"; exit 1; }
echo "✓ Build successful"

# Install binary
echo ""
echo "Installing..."
mkdir -p "$INSTALL_DIR/bin"
cp bin/Release/net9.0/VB "$INSTALL_DIR/bin/"
cp bin/Release/net9.0/*.dll "$INSTALL_DIR/bin/" 2>/dev/null || true
chmod +x "$INSTALL_DIR/bin/VB"

# Install VML files (copied flat to bin for runtime)
cp vml/*.vml "$INSTALL_DIR/bin/"

# Setup runtime database
mkdir -p "$DB_DIR"
cp "${DB_FILE}" "$DB_DIR/visualised.db"

if [ "$SYSTEM_INSTALL" = true ]; then
    chmod 755 "$DB_DIR"
    chmod 644 "$DB_DIR/visualised.db"
else
    chmod 700 "$DB_DIR"
    chmod 600 "$DB_DIR/visualised.db"
fi

# Add to PATH (user install only)
if [ "$SYSTEM_INSTALL" = false ]; then
    if ! grep -q "$INSTALL_DIR/bin" "$HOME/.bashrc" 2>/dev/null; then
        echo "" >> "$HOME/.bashrc"
        echo "# Visualised Markup" >> "$HOME/.bashrc"
        echo "export PATH=\\$PATH:$INSTALL_DIR/bin\" >> "$HOME/.bashrc"
        echo ""
        echo "→ Added $INSTALL_DIR/bin to PATH in ~/.bashrc"
        echo "  Run: source ~/.bashrc"
    fi
fi

# Cleanup
cd /
rm -rf /tmp/vb-build

echo ""
echo "╔════════════════════════════════════════╗"
echo "║  Installation Complete! ✓              ║"
echo "╚════════════════════════════════════════╝"
echo ""
echo "Binary:   $INSTALL_DIR/bin/VB"
echo "Database: $DB_DIR/visualised.db"
echo ""
if [ "$SYSTEM_INSTALL" = false ]; then
    echo "Run: source ~/.bashrc && VB"
else
    echo "Run: VB"
fi
echo ""
echo "The database contains full source - VB is self-replicating!"
