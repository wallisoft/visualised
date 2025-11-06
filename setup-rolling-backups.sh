#!/bin/bash
# Rolling 30-day backup system for VML and CS files

DB="vb-source.db"
BACKUP_DIR="../chaff/db-backups"
MAX_BACKUPS=30

echo "📦 Setting up rolling backup system..."
echo ""

# Function to backup a file
backup_file() {
    local filename=$1
    local category=$2
    local timestamp=$(date +%Y%m%d_%H%M%S)
    local backup_path="$BACKUP_DIR/${filename}.${timestamp}.backup"
    
    # Extract and backup
    if [ "$category" = "vml" ]; then
        sqlite3 "$DB" "SELECT content FROM vml_files WHERE filename='$filename'" > "$backup_path"
    else
        sqlite3 "$DB" "SELECT content FROM source_files WHERE filename='$filename'" > "$backup_path"
    fi
    
    echo "  ✓ Backed up: $filename"
    
    # Clean old backups (keep only last 30)
    local count=$(ls -1 "$BACKUP_DIR/${filename}."*.backup 2>/dev/null | wc -l)
    if [ $count -gt $MAX_BACKUPS ]; then
        local to_delete=$((count - MAX_BACKUPS))
        ls -1t "$BACKUP_DIR/${filename}."*.backup | tail -$to_delete | xargs rm -f
        echo "    Cleaned $to_delete old backups"
    fi
}

# Backup key CS files
echo "Backing up CS files..."
backup_file "DesignerWindow.cs" "source"
backup_file "PropertiesPanel.cs" "source"
backup_file "MainWindow.axaml.cs" "source"
backup_file "VmlUiBuilder.cs" "source"

echo ""
echo "Backing up VML files..."
backup_file "designer.vml" "vml"
backup_file "visual-script-editor.vml" "vml"

echo ""
echo "════════════════════════════════════════"
echo "✓ BACKUP SYSTEM CONFIGURED"
echo "════════════════════════════════════════"
echo ""
echo "Configuration:"
echo "  • Location: $BACKUP_DIR"
echo "  • Max backups per file: $MAX_BACKUPS"
echo "  • Auto-cleanup: Yes"
echo ""
echo "Backed up files:"
ls -lh "$BACKUP_DIR" | tail -10
echo ""
echo "Before release, run:"
echo "  rm -rf $BACKUP_DIR/*"
echo "  # Clean all dev backups"
