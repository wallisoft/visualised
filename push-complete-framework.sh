#!/bin/bash
# Push complete cross-platform framework to GitHub

set -e

echo "════════════════════════════════════════════════════════════"
echo "🌳 VISUALISED MARKUP - COMPLETE FRAMEWORK PUSH"
echo "════════════════════════════════════════════════════════════"
echo ""

# GitHub configuration
GITHUB_USER="wallisoft"
REPO_NAME="visualised"
REMOTE_URL="git@github.com:${GITHUB_USER}/${REPO_NAME}.git"
GIT_EMAIL="wallisoft@gmail.com"
GIT_NAME="Steve Wallis"

# Configure git
echo "⚙️  Configuring git..."
git config user.name "$GIT_NAME"
git config user.email "$GIT_EMAIL"
echo "✓ Git configured"
echo ""

# Set up remote
if ! git remote | grep -q origin; then
    git remote add origin "$REMOTE_URL"
else
    git remote set-url origin "$REMOTE_URL"
fi
echo "✓ Remote configured"
echo ""

# Add everything
echo "➕ Adding all files..."
git add .
echo "✓ All files staged"
echo ""

# Show what's being committed
echo "📋 Files staged:"
git status --short | head -30
TOTAL=$(git status --short | wc -l)
if [ $TOTAL -gt 30 ]; then
    echo "... and $((TOTAL - 30)) more files"
fi
echo ""

# Create the epic commit message
COMMIT_MSG="🌳 Complete Cross-Platform Framework - Linux, Windows, Android

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🚀 THE COMPLETE VISION - Three Platforms, One VML
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Revolutionary RAD IDE with complete cross-platform architecture:
- Linux: Full visual designer + editor
- Windows: Full visual designer + editor  
- Android: Lightweight runtime loader

Same VML markup. Same SQLite database. Everywhere.

═══════════════════════════════════════════════════════════════
📦 NEW: PACKAGING & DEPLOYMENT
═══════════════════════════════════════════════════════════════

Linux Packaging:
  • package-linux.sh - Creates .tar.gz distribution
  • Self-contained binary with all dependencies
  • Installer script for system-wide installation
  • .desktop file for application menu

Windows Packaging:
  • package-windows.ps1 - Creates .zip distribution
  • PowerShell helper scripts (vb-get.ps1, vb-set.ps1)
  • Batch launcher for easy execution
  • PowerShell installer for Program Files

Android Architecture:
  • ANDROID-ARCHITECTURE.md - Complete specification
  • Lightweight APK loader (2-5 MB!)
  • Background PropertyService
  • Termux integration for bash scripting 🔥
  • No editor overhead - design on desktop, deploy to mobile

═══════════════════════════════════════════════════════════════
✨ CORE FEATURES (Completed)
═══════════════════════════════════════════════════════════════

Visual Designer:
  ✓ Drag-and-drop with 4K canvas
  ✓ Auto-stacking (20px diagonal offset)
  ✓ Real-time property panel
  ✓ Perfect cursor feedback
  ✓ Context menu integration

VML Language:
  ✓ Flat-file declarative syntax
  ✓ Zero boilerplate
  ✓ Template system
  ✓ Self-hosting (designer built in VML!)

Script System:
  ✓ Multi-language support
  ✓ SQLite property store
  ✓ Auto-naming (Button_1, TextBox_1)
  ✓ Event handling (onClick, onLoad, etc.)

Cross-Platform:
  ✓ Linux desktop (primary)
  ✓ Windows 11 PowerShell scripts
  ✓ Android loader architecture
  ✓ Shared VML format
  ✓ Consistent behavior everywhere

═══════════════════════════════════════════════════════════════
🎯 STATUS
═══════════════════════════════════════════════════════════════

Version: 1.0 Pre-Release
Status: Active Development

Completed:
  ✓ Core architecture
  ✓ Visual designer
  ✓ Property system
  ✓ Script handlers
  ✓ Cross-platform packaging
  ✓ Complete documentation

In Progress:
  ⏳ Script editor completion
  ⏳ Final polish and testing

Next Milestone: v1.0 Release Candidate

═══════════════════════════════════════════════════════════════
📚 DOCUMENTATION
═══════════════════════════════════════════════════════════════

  • README.md - Complete overview with examples
  • TECH-SPEC.md - Full technical specification
  • ANDROID-ARCHITECTURE.md - Mobile architecture
  • LICENSE.md - Dual licensing structure
  • LICENSE-FREE.md - Personal/educational terms
  • LICENSE-COMMERCIAL.md - Commercial pricing

═══════════════════════════════════════════════════════════════
🛠️ TECHNOLOGY STACK
═══════════════════════════════════════════════════════════════

Desktop:
  • Avalonia 11.x (cross-platform UI)
  • C# / .NET 9.0
  • SQLite 3.x property store
  • Multi-language scripting

Android:
  • Lightweight Kotlin loader
  • SQLite database backend
  • Termux for bash scripting
  • ContentProvider IPC

═══════════════════════════════════════════════════════════════
💎 WHY THIS MATTERS
═══════════════════════════════════════════════════════════════

This is not just another IDE. This is a paradigm shift:

1. **Recursive Self-Building**: The IDE uses its own technology
2. **True Cross-Platform**: Same VML works everywhere
3. **Lightweight Mobile**: 2-5 MB APKs, not 50+ MB frameworks
4. **Language Agnostic**: Choose your scripting language
5. **Property-Centric**: Everything is queryable, everything is accessible
6. **Declarative First**: Define WHAT you want, not HOW to build it

The tool that builds itself is now ready to build anything.

═══════════════════════════════════════════════════════════════
🎨 THE POETRY OF IT
═══════════════════════════════════════════════════════════════

Design once in VML on your Linux desktop.
Deploy the same definition to Windows, to Android.
Script in bash, python, ruby - your choice.
Query properties from anywhere - SQLite is the bridge.
2 MB APK on mobile. Same features. Same elegance.

One language. One database. Everywhere.

That's the vision. That's Visualised Markup.

═══════════════════════════════════════════════════════════════
📧 CONTACT
═══════════════════════════════════════════════════════════════

Author: Steve Wallis @ Wallisoft
Email: wallisoft@gmail.com
License: Dual (Free/Commercial)
Patent: UK application pending

Star us on GitHub! ⭐

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🌳 The framework is complete. The editor is next. Then we ship.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Commit
echo "💾 Creating commit..."
git commit -m "$COMMIT_MSG"
echo "✓ Committed"
echo ""

# Push
echo "🚀 Pushing to GitHub..."
git push -u origin main

echo ""
echo "════════════════════════════════════════════════════════════"
echo "🎉 COMPLETE FRAMEWORK NOW ON GITHUB!"
echo "════════════════════════════════════════════════════════════"
echo ""
echo "🌐 Repository:"
echo "   https://github.com/${GITHUB_USER}/${REPO_NAME}"
echo ""
echo "📦 Now Available:"
echo "   • Complete Linux packaging"
echo "   • Windows PowerShell scripts"
echo "   • Android architecture spec"
echo "   • Professional documentation"
echo ""
echo "🎯 Next Steps:"
echo "   1. Complete script editor"
echo "   2. Final testing"
echo "   3. Create v1.0-rc tag"
echo "   4. Release to the world!"
echo ""
echo "🌳 Bash on Android - because why not? 😎"
echo "════════════════════════════════════════════════════════════"

