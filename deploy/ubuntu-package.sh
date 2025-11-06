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
