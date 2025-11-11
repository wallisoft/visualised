# VML Dev Session Quick Start

## API Access
curl -s -H "X-API-Key: dev-1762055196" -X POST "http://tmp.avalised.io:8889/shell" -d "COMMAND"

## Working Directory
~/Downloads/visualised/

## Critical Rules
1. **You're on my server** (not container) - use dev API
2. **No deletes** - give bash for review
3. **No architecture changes** without discussion  
4. **Token frugal** - gzip >20KB, minimal info retrieval

## Database Schema
sqlite3 visualised.db:
- source_files (path, content, file_type)
- properties (control_name, property_name, property_value)
- apps (name, vml_content)

## Docs Already Read
README.md, INSTALL.md, TECH-SPEC.md (all in db and git)

## Current Focus
[Update this each session with what you're working on]

## Quick DB Queries
# List all source files
sqlite3 visualised.db "SELECT path, file_type FROM source_files ORDER BY file_type, path;"

# Get specific file
sqlite3 visualised.db "SELECT content FROM source_files WHERE path='filename';"

# File count by type
sqlite3 visualised.db "SELECT file_type, COUNT(*) FROM source_files GROUP BY file_type;"
