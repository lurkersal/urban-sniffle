#!/bin/bash
# Restore the magazine database schema

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCHEMA_FILE="$SCRIPT_DIR/schema_postgres.sql"

echo "Dropping and recreating schema..."
PGPASSWORD=Barnowl1 psql -U postgres -h localhost -d magazines -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"

echo "Restoring schema from $SCHEMA_FILE..."
PGPASSWORD=Barnowl1 psql -U postgres -h localhost -d magazines -f "$SCHEMA_FILE"

echo "Database restored successfully!"
