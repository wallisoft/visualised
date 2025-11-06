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
