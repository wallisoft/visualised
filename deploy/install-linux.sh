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
