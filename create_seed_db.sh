#!/bin/bash
# Create minimal seed database with schema only

sqlite3 seed.db << EOF
-- Properties table for runtime control state
CREATE TABLE IF NOT EXISTS properties (
    control_name TEXT NOT NULL,
    property_name TEXT NOT NULL,
    property_value TEXT,
    PRIMARY KEY (control_name, property_name)
);

-- Future: Apps table for installed VML applications
CREATE TABLE IF NOT EXISTS apps (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    vml_content TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Insert seed comment
INSERT INTO properties VALUES ("_system", "_version", "1.0");
EOF

ls -lh seed.db
