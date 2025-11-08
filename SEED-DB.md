# seed.db

Minimal SQLite database with schema for Visualised Markup runtime.

## Schema

**properties** - Runtime control state
- control_name (TEXT) - Control identifier
- property_name (TEXT) - Property name  
- property_value (TEXT) - Property value
- PRIMARY KEY (control_name, property_name)

**apps** - Installed VML applications (future)
- id (INTEGER) - Auto-increment ID
- name (TEXT) - Application name
- vml_content (TEXT) - VML definition
- created_at (DATETIME) - Install timestamp

## Usage

On first run, VB copies seed.db to:
- System install: `/var/lib/visualised/visualised.db`
- User install: `~/.visualised/visualised.db`

The runtime database persists across sessions.
