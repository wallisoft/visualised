#!/bin/bash
echo "🧹 PRE-RELEASE CLEANUP"
echo ""

read -p "Clear all backups from ../chaff/db-backups? (y/n) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    rm -rf ../chaff/db-backups/*
    echo "✓ Backups cleared"
    
    rm -rf bin/ obj/
    echo "✓ Build artifacts cleared"
    
    echo ""
    echo "Clean for release! 🚀"
fi
