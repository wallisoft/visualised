#!/bin/bash
set -e

REPO="https://github.com/wallisoft/visualised"
INSTALL_DIR="${INSTALL_DIR:-$HOME/.local}"
SYSTEM_INSTALL=false

echo "╔════════════════════════════════════════╗"
echo "║  Visualised Markup Installer v1.0     ║"
echo "╚════════════════════════════════════════╝"
echo ""

# Check for sudo/root for system install
if [ "$EUID" -eq 0 ] || [ "$1" = "--system" ]; then
    SYSTEM_INSTALL=true
    INSTALL_DIR="/usr/local"
    DB_DIR="/var/lib/visualised"
    echo "→ System-wide install to $INSTALL_DIR"
else
    DB_DIR="$HOME/.visualised"
    echo "→ User install to $INSTALL_DIR"
fi

# Check dependencies
echo ""
echo "Checking dependencies..."
command -v dotnet >/dev/null 2>&1 || { echo "✗ .NET 9 SDK required. Install from: https://dot.net"; exit 1; }
command -v git >/dev/null 2>&1 || { echo "✗ git required"; exit 1; }
echo "✓ Dependencies OK"

# Clone or download
echo ""
if [ ! -d "visualised-src" ]; then
    echo "Downloading source..."
    git clone --depth 1 "$REPO" visualised-src
else
    echo "Using existing source (visualised-src/)"
fi

cd visualised-src

# Build
echo ""
echo "Building VB..."
dotnet build -c Release >/dev/null 2>&1 || { echo "✗ Build failed"; exit 1; }
echo "✓ Build successful"

# Install binary
echo ""
echo "Installing binary..."
mkdir -p "$INSTALL_DIR/bin"
cp bin/Release/net9.0/VB "$INSTALL_DIR/bin/"
cp bin/Release/net9.0/*.dll "$INSTALL_DIR/bin/" 2>/dev/null || true
chmod +x "$INSTALL_DIR/bin/VB"
echo "✓ Installed to $INSTALL_DIR/bin/VB"

# Install VML files
echo "Installing VML files..."
mkdir -p "$INSTALL_DIR/share/visualised/vml"
cp vml/*.vml "$INSTALL_DIR/share/visualised/vml/"
# Copy VML flat to bin directory for runtime access
cp vml/*.vml "$INSTALL_DIR/bin/"
echo "✓ VML files installed"

# Setup database
echo ""
echo "Setting up database..."
mkdir -p "$DB_DIR"
if [ -f "seed.db" ]; then
    cp seed.db "$DB_DIR/visualised.db"
else
    echo "Warning: seed.db not found, creating empty database"
    sqlite3 "$DB_DIR/visualised.db" "CREATE TABLE properties (control_name TEXT, property_name TEXT, property_value TEXT, PRIMARY KEY (control_name, property_name));"
fi

if [ "$SYSTEM_INSTALL" = true ]; then
    chmod 755 "$DB_DIR"
    chmod 644 "$DB_DIR/visualised.db"
else
    chmod 700 "$DB_DIR"
    chmod 600 "$DB_DIR/visualised.db"
fi
echo "✓ Database: $DB_DIR/visualised.db"

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

echo ""
echo "╔════════════════════════════════════════╗"
echo "║  Installation Complete! ✓              ║"
echo "╚════════════════════════════════════════╝"
echo ""
echo "Binary:   $INSTALL_DIR/bin/VB"
echo "Database: $DB_DIR/visualised.db"
echo "VML:      $INSTALL_DIR/share/visualised/vml/"
echo ""
if [ "$SYSTEM_INSTALL" = false ]; then
    echo "Run: source ~/.bashrc && VB"
else
    echo "Run: VB"
fi
echo ""
