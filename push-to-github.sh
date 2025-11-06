#!/bin/bash
echo "🚀 PUSHING TO GITHUB"
echo "════════════════════════════════════════"
echo ""

# Git configuration
git config user.name "Steve Wallis"
git config user.email "register@wallisoft.uk"

echo "✓ Git configured"
echo "  Name: Steve Wallis"
echo "  Email: register@wallisoft.uk"
echo ""

# Check if remote exists
if git remote | grep -q "origin"; then
    echo "Remote 'origin' exists, updating..."
    git remote set-url origin git@github.com:wallisoft/visualised.git
else
    echo "Adding remote 'origin'..."
    git remote add origin git@github.com:wallisoft/visualised.git
fi

echo "✓ Remote: git@github.com:wallisoft/visualised.git"
echo ""

# Verify SSH connection
echo "Testing SSH connection to GitHub..."
ssh -T git@github.com 2>&1 | grep -q "successfully authenticated" && echo "✓ SSH authenticated!" || echo "⚠ SSH test (this is normal if key is set up)"

echo ""
echo "════════════════════════════════════════"
echo "STEP 1: Fetch current repo state"
echo "════════════════════════════════════════"
echo ""

git fetch origin main --depth=1 2>/dev/null || echo "No existing main branch or empty repo"

echo ""
echo "════════════════════════════════════════"
echo "STEP 2: Squash everything into RC 1.0"
echo "════════════════════════════════════════"
echo ""

# Create comprehensive commit message
cat > /tmp/commit-msg << 'MSG'
Release Candidate 1.0.0 - Visualised Markup RAD IDE

Revolutionary YAML-driven RAD IDE that recursively builds itself.

FEATURES:
- 8-direction seamless resize (all edges + corners)
- Auto-numbered controls (Button_1, TextBox_1, etc)
- Visual script editor (retro green on black)
- Live property editing panel
- SQLite runtime database (/tmp/vb-runtime.db)
- Multi-language scripts (bash, python, node, ruby, perl)
- Context menus with full control operations
- Z-order management (bring to front/send to back)
- 4K scrollable canvas with 800x600 guide overlay
- Cross-platform (Linux, Mac, Windows)

ARCHITECTURE:
- C# 12 / .NET 9
- Avalonia UI 11.2.1 (cross-platform)
- SQLite database (self-contained source)
- VML (Visual Markup Language) configuration
- Recursive self-building capability
- Database-driven development workflow

PATENT:
UK Patent Application Filed (20+ claims)
Revolutionary self-modifying IDE architecture

DEPLOYMENT:
- Linux/Mac installer: deploy/install-linux.sh
- Windows installer: deploy/install-windows.bat
- Ubuntu .deb package: deploy/ubuntu-package.sh
- Windows 11 winget manifest ready

DATABASE ARCHITECTURE:
- vb-source.db: Complete source code storage
- stub.sh: Extraction and build script
- Build from scratch in seconds
- Version control via database snapshots

DOMAINS:
- visualised.io (primary)
- visualised.org (docs)
- Cloudflare DNS ready

AUTHOR:
Steve Wallis @ Wallisoft
register@wallisoft.uk

Eastbourne, England, UK
November 2025
MSG

# Squash all commits into one
echo "Creating squashed commit..."

# If there's existing history, create orphan branch
git checkout --orphan new-main

# Add everything
git add -A

# Commit with comprehensive message
git commit -F /tmp/commit-msg

echo "✓ Squashed into single commit"

echo ""
echo "════════════════════════════════════════"
echo "STEP 3: Push to GitHub (force)"
echo "════════════════════════════════════════"
echo ""

echo "This will:"
echo "  • Replace main branch with new-main"
echo "  • Squash all history into one commit"
echo "  • Keep the repo but with clean history"
echo ""

read -p "Push to GitHub? (y/n) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    # Delete old main and push new-main as main
    git branch -D main 2>/dev/null || true
    git branch -m new-main main
    
    echo "Pushing to GitHub..."
    git push -u origin main --force
    
    if [ $? -eq 0 ]; then
        echo ""
        echo "✓ Pushed to GitHub!"
    else
        echo ""
        echo "❌ Push failed - check SSH keys"
        echo ""
        echo "To fix SSH:"
        echo "  1. Generate key: ssh-keygen -t ed25519 -C 'register@wallisoft.uk'"
        echo "  2. Copy key: cat ~/.ssh/id_ed25519.pub"
        echo "  3. Add to GitHub: Settings → SSH Keys"
        exit 1
    fi
fi

echo ""
echo "════════════════════════════════════════"
echo "STEP 4: Create release tag"
echo "════════════════════════════════════════"
echo ""

git tag -a v1.0.0 -m "Release Candidate 1.0.0

First public release of Visualised Markup RAD IDE.

Features complete:
- Visual designer with 8-direction resize
- Script editor with multi-language support  
- Property panel with live editing
- SQLite runtime database
- Cross-platform deployment
- Self-contained database architecture

Ready for production testing."

git push origin v1.0.0

echo "✓ Tagged as v1.0.0"

echo ""
echo "════════════════════════════════════════"
echo "✓ GITHUB PUSH COMPLETE!"
echo "════════════════════════════════════════"
echo ""
echo "Repository: https://github.com/wallisoft/visualised"
echo "Release: https://github.com/wallisoft/visualised/releases/tag/v1.0.0"
echo ""
