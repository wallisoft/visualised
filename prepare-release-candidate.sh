#!/bin/bash
echo "🚀 PREPARING RELEASE CANDIDATE"
echo "════════════════════════════════════════"
echo ""

DB="vb-source.db"

# ═══════════════════════════════════════════
echo "STEP 1: Update database with current files"
echo "════════════════════════════════════════"
echo ""

# Get list of all CS files
for file in *.cs; do
    if [ -f "$file" ]; then
        echo "  → $file"
        sqlite3 "$DB" "INSERT OR REPLACE INTO source_files (filename, content, category) 
                       VALUES ('$file', readfile('$file'), 'source');"
    fi
done

# Get VML files
for file in *.vml; do
    if [ -f "$file" ]; then
        echo "  → $file"
        sqlite3 "$DB" "INSERT OR REPLACE INTO vml_files (filename, content, description) 
                       VALUES ('$file', readfile('$file'), 'VML configuration');"
    fi
done

# Get project files
for file in *.csproj *.axaml; do
    if [ -f "$file" ]; then
        echo "  → $file"
        sqlite3 "$DB" "INSERT OR REPLACE INTO source_files (filename, content, category) 
                       VALUES ('$file', readfile('$file'), 'project');"
    fi
done

echo ""
echo "✓ Database updated with all current files"

# ═══════════════════════════════════════════
echo ""
echo "STEP 2: Verify database contents"
echo "════════════════════════════════════════"
echo ""

echo "Source files in database:"
sqlite3 "$DB" "SELECT COUNT(*), category FROM source_files GROUP BY category;"

echo ""
echo "VML files in database:"
sqlite3 "$DB" "SELECT COUNT(*) FROM vml_files;"

# ═══════════════════════════════════════════
echo ""
echo "STEP 3: Nuclear cleanup (keep only db + stub)"
echo "════════════════════════════════════════"
echo ""

read -p "Delete EVERYTHING except vb-source.db and stub.sh? (y/n) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    # List of files to KEEP
    KEEP=("vb-source.db" "stub.sh" "prepare-release-candidate.sh")
    
    echo "Keeping only:"
    for file in "${KEEP[@]}"; do
        echo "  ✓ $file"
    done
    
    echo ""
    echo "Removing everything else..."
    
    # Remove all files except keepers
    for item in *; do
        keep=false
        for keeper in "${KEEP[@]}"; do
            if [ "$item" == "$keeper" ]; then
                keep=true
                break
            fi
        done
        
        if [ "$keep" = false ]; then
            echo "  💀 $item"
            rm -rf "$item"
        fi
    done
    
    # Remove build directories
    rm -rf bin/ obj/ build/
    
    echo ""
    echo "✓ Nuclear cleanup complete!"
    echo ""
    ls -lh
fi

# ═══════════════════════════════════════════
echo ""
echo "STEP 4: Build from stub (ultimate test)"
echo "════════════════════════════════════════"
echo ""

read -p "Extract from database and build? (y/n) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    ./stub.sh
    
    echo ""
    echo "Building..."
    dotnet build
    
    if [ $? -eq 0 ]; then
        echo ""
        echo "════════════════════════════════════════"
        echo "✓ BUILD FROM STUB SUCCESSFUL!"
        echo "════════════════════════════════════════"
        echo ""
        echo "Run and test:"
        echo "  ./bin/Debug/net9.0/VB"
        echo ""
        read -p "Test now? Press Enter when done testing..."
    else
        echo ""
        echo "❌ Build failed - fix before release!"
        exit 1
    fi
fi

# ═══════════════════════════════════════════
echo ""
echo "STEP 5: Create deploy scripts"
echo "════════════════════════════════════════"
echo ""

mkdir -p deploy

# Windows installer stub
cat > deploy/install-windows.bat << 'WINBAT'
@echo off
echo Visualised Markup - Windows Installation
echo ========================================
echo.

REM Check for .NET 9
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET 9 SDK not found!
    echo Please install from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Extracting source from database...
bash stub.sh

echo Building application...
dotnet build -c Release

if errorlevel 1 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Installation complete!
echo ========================================
echo.
echo Run: bin\Release\net9.0\VB.exe
echo.
pause
WINBAT

# Linux/Mac installer
cat > deploy/install-linux.sh << 'LINUXSH'
#!/bin/bash
echo "Visualised Markup - Linux/Mac Installation"
echo "=========================================="
echo ""

# Check for .NET 9
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET 9 SDK not found!"
    echo "Install from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "Extracting source from database..."
./stub.sh

echo "Building application..."
dotnet build -c Release

if [ $? -eq 0 ]; then
    echo ""
    echo "=========================================="
    echo "Installation complete!"
    echo "=========================================="
    echo ""
    echo "Run: ./bin/Release/net9.0/VB"
    echo ""
else
    echo ""
    echo "ERROR: Build failed!"
    exit 1
fi
LINUXSH

chmod +x deploy/install-linux.sh

# Ubuntu package manager script
cat > deploy/ubuntu-package.sh << 'UBUNTU'
#!/bin/bash
# Build .deb package for Ubuntu

VERSION="1.0.0"
PACKAGE="visualised-markup"
ARCH="amd64"

echo "Building Ubuntu .deb package..."
echo ""

# Create package structure
mkdir -p "${PACKAGE}_${VERSION}_${ARCH}/DEBIAN"
mkdir -p "${PACKAGE}_${VERSION}_${ARCH}/usr/local/bin"
mkdir -p "${PACKAGE}_${VERSION}_${ARCH}/usr/share/${PACKAGE}"

# Control file
cat > "${PACKAGE}_${VERSION}_${ARCH}/DEBIAN/control" << CONTROL
Package: ${PACKAGE}
Version: ${VERSION}
Section: devel
Priority: optional
Architecture: ${ARCH}
Depends: dotnet-sdk-9.0
Maintainer: Wallisoft <info@wallisoft.com>
Description: Visualised Markup RAD IDE
 YAML-driven RAD IDE that recursively builds itself.
 Revolutionary visual development environment.
CONTROL

# Copy files
cp vb-source.db "${PACKAGE}_${VERSION}_${ARCH}/usr/share/${PACKAGE}/"
cp stub.sh "${PACKAGE}_${VERSION}_${ARCH}/usr/share/${PACKAGE}/"

# Launcher script
cat > "${PACKAGE}_${VERSION}_${ARCH}/usr/local/bin/visualised-markup" << LAUNCHER
#!/bin/bash
cd /usr/share/${PACKAGE}
./stub.sh
dotnet build
./bin/Debug/net9.0/VB
LAUNCHER

chmod +x "${PACKAGE}_${VERSION}_${ARCH}/usr/local/bin/visualised-markup"

# Build package
dpkg-deb --build "${PACKAGE}_${VERSION}_${ARCH}"

echo "✓ Package created: ${PACKAGE}_${VERSION}_${ARCH}.deb"
UBUNTU

chmod +x deploy/ubuntu-package.sh

# Windows 11 package manager (winget) manifest
mkdir -p deploy/winget
cat > deploy/winget/visualised-markup.yaml << 'WINGET'
PackageIdentifier: Wallisoft.VisualisedMarkup
PackageVersion: 1.0.0
PackageName: Visualised Markup
Publisher: Wallisoft
License: MIT
ShortDescription: YAML-driven RAD IDE
Description: Revolutionary visual development environment that recursively builds itself
PackageUrl: https://github.com/wallisoft/visualised-markup
InstallerType: portable
Installers:
  - Architecture: x64
    InstallerUrl: https://github.com/wallisoft/visualised-markup/releases/download/v1.0.0/VB.exe
    InstallerSha256: <TO_BE_FILLED>
ManifestType: singleton
ManifestVersion: 1.0.0
WINGET

echo "✓ Deploy scripts created in ./deploy/"

# ═══════════════════════════════════════════
echo ""
echo "STEP 6: Initialize Git repository"
echo "════════════════════════════════════════"
echo ""

read -p "Initialize Git repo and commit? (y/n) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    # Initialize if needed
    if [ ! -d ".git" ]; then
        git init
        echo "✓ Git initialized"
    fi
    
    # Create .gitignore
    cat > .gitignore << 'IGNORE'
# Build output
bin/
obj/
build/

# User files
*.user
*.suo
*.cache

# OS files
.DS_Store
Thumbs.db

# IDE
.vs/
.vscode/
.idea/

# Temp files
*.tmp
*.bak
*~

# Keep database and stub!
!vb-source.db
!stub.sh
IGNORE
    
    # Create README
    cat > README.md << 'README'
# Visualised Markup

Revolutionary YAML-driven RAD IDE that recursively builds itself.

## Features

- **8-direction seamless resize** - Native feel
- **Auto-numbered controls** - Button_1, TextBox_1, etc
- **Visual script editor** - Black screen, green text
- **Property panel** - Live editing
- **SQLite runtime** - vb-get/vb-set scripts
- **Multi-language support** - bash, python, node, ruby, perl

## Installation

### Linux/Mac
```bash
chmod +x deploy/install-linux.sh
./deploy/install-linux.sh
```

### Windows
```cmd
deploy\install-windows.bat
```

### Ubuntu Package
```bash
cd deploy
./ubuntu-package.sh
sudo dpkg -i visualised-markup_1.0.0_amd64.deb
```

## Quick Start

The entire application is contained in:
- `vb-source.db` - SQLite database with all source code
- `stub.sh` - Extraction script

Build from database:
```bash
./stub.sh
dotnet build
./bin/Debug/net9.0/VB
```

## Architecture

- **C# Avalonia** - Cross-platform UI
- **SQLite** - Self-contained source storage
- **VML (YAML)** - Visual markup language
- **Recursive** - IDE builds itself

## Patent Pending

UK Patent Application Filed - 20+ claims

## License

MIT License - see LICENSE file

## Author

Steve @ Wallisoft
README
    
    # Add all files
    git add .
    
    # Initial commit
    git commit -m "Release Candidate 1.0.0

Features:
- 8-direction seamless resize
- Auto-numbered controls (Button_1, etc)
- Visual script editor with syntax highlighting
- Property panel with live editing
- SQLite runtime database
- Multi-language script support (bash, python, node, ruby, perl)
- Cross-platform (Linux, Mac, Windows)
- Self-contained database architecture
- Deploy scripts for all platforms

Architecture:
- C# Avalonia UI framework
- SQLite source storage
- VML (Visual Markup Language)
- Recursive self-building capability

Patent: UK Application Filed (20+ claims)"
    
    echo ""
    echo "✓ Git commit created"
fi

# ═══════════════════════════════════════════
echo ""
echo "STEP 7: GitHub setup"
echo "════════════════════════════════════════"
echo ""

echo "To push to GitHub:"
echo ""
echo "1. Create repo on GitHub: visualised-markup"
echo ""
echo "2. Run these commands:"
echo "   git remote add origin https://github.com/YOUR_USERNAME/visualised-markup.git"
echo "   git branch -M main"
echo "   git push -u origin main --force  # Squash existing"
echo ""
echo "Or if you want to squash everything:"
echo "   git reset \$(git commit-tree HEAD^{tree} -m 'Release Candidate 1.0.0')"
echo "   git push -u origin main --force"
echo ""

# ═══════════════════════════════════════════
echo ""
echo "════════════════════════════════════════"
echo "🎉 RELEASE CANDIDATE READY!"
echo "════════════════════════════════════════"
echo ""
echo "Repository contains:"
ls -lh
echo ""
echo "Deploy scripts:"
ls -lh deploy/
echo ""
echo "Next steps:"
echo "  1. Test the application thoroughly"
echo "  2. Push to GitHub"
echo "  3. Create release tag: v1.0.0"
echo "  4. Attach binaries to release"
echo ""
echo "🚀 Ready for launch! 🌳"
