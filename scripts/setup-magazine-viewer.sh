#!/bin/bash
# Setup local development environment for magazine-viewer
# This script creates a local launchSettings file with your credentials

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/../src/magazine-viewer"
PROPS_DIR="$PROJECT_DIR/Properties"
LOCAL_FILE="$PROPS_DIR/launchSettings.local.json"
TEMPLATE_FILE="$PROPS_DIR/launchSettings.local.json.template"

echo "========================================="
echo "Magazine Viewer - Local Setup"
echo "========================================="
echo ""

# Check if local file already exists
if [ -f "$LOCAL_FILE" ]; then
    echo "⚠️  launchSettings.local.json already exists!"
    read -p "Do you want to overwrite it? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Setup cancelled."
        exit 0
    fi
fi

# Get database credentials
echo "Database Configuration"
echo "----------------------"
read -p "Database host [localhost]: " DB_HOST
DB_HOST=${DB_HOST:-localhost}

read -p "Database name [magazines]: " DB_NAME
DB_NAME=${DB_NAME:-magazines}

read -p "Database username [postgres]: " DB_USER
DB_USER=${DB_USER:-postgres}

read -sp "Database password: " DB_PASSWORD
echo ""

# Get image root
echo ""
echo "Image Configuration"
echo "-------------------"
read -p "Magazine image root path: " IMAGE_ROOT

# Create the local settings file
echo ""
echo "Creating launchSettings.local.json..."

cat > "$LOCAL_FILE" <<EOF
{
  "\$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "environmentVariables": {
        "MAGAZINE_DB": "Host=${DB_HOST};Username=${DB_USER};Password=${DB_PASSWORD};Database=${DB_NAME}",
        "MAGAZINE_IMAGE_ROOT": "${IMAGE_ROOT}"
      }
    },
    "https": {
      "commandName": "Project",
      "environmentVariables": {
        "MAGAZINE_DB": "Host=${DB_HOST};Username=${DB_USER};Password=${DB_PASSWORD};Database=${DB_NAME}",
        "MAGAZINE_IMAGE_ROOT": "${IMAGE_ROOT}"
      }
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "environmentVariables": {
        "MAGAZINE_DB": "Host=${DB_HOST};Username=${DB_USER};Password=${DB_PASSWORD};Database=${DB_NAME}",
        "MAGAZINE_IMAGE_ROOT": "${IMAGE_ROOT}"
      }
    }
  }
}
EOF

echo "✅ launchSettings.local.json created successfully!"
echo ""
echo "This file is gitignored and will NOT be committed."
echo "You can now run magazine-viewer in Rider with your credentials."
echo ""
echo "Alternative: You can also use User Secrets:"
echo "  cd $PROJECT_DIR"
echo "  dotnet user-secrets set \"ConnectionStrings:MagazineDb\" \"Host=${DB_HOST};Username=${DB_USER};Password=<your-password>;Database=${DB_NAME}\""
echo ""

