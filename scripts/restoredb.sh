#!/bin/bash
# Restore the magazine database schema

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCHEMA_FILE="$SCRIPT_DIR/schema_postgres.sql"

# Check if password is provided via environment
if [ -z "$MAGAZINE_DB_PASSWORD" ]; then
    echo "❌ Error: MAGAZINE_DB_PASSWORD environment variable not set"
    echo ""
    echo "Usage:"
    echo "  MAGAZINE_DB_PASSWORD=your_password ./restoredb.sh"
    echo ""
    echo "Or set in your environment:"
    echo "  export MAGAZINE_DB_PASSWORD=your_password"
    echo "  ./restoredb.sh"
    exit 1
fi

# Database connection parameters (with sensible defaults)
DB_HOST="${MAGAZINE_DB_HOST:-localhost}"
DB_USER="${MAGAZINE_DB_USER:-postgres}"
DB_NAME="${MAGAZINE_DB_NAME:-magazines}"

echo "🔄 Dropping and recreating schema on $DB_HOST..."
PGPASSWORD="$MAGAZINE_DB_PASSWORD" psql -U "$DB_USER" -h "$DB_HOST" -d "$DB_NAME" \
    -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"

if [ $? -ne 0 ]; then
    echo "❌ Failed to drop schema"
    exit 1
fi

echo "📥 Restoring schema from $SCHEMA_FILE..."
PGPASSWORD="$MAGAZINE_DB_PASSWORD" psql -U "$DB_USER" -h "$DB_HOST" -d "$DB_NAME" \
    -f "$SCHEMA_FILE"

if [ $? -ne 0 ]; then
    echo "❌ Failed to restore schema"
    exit 1
fi

echo "✅ Database restored successfully!"
