#!/bin/bash
# Quick setup script for database configuration
# Run this after cloning the repository

set -e  # Exit on error

echo "================================================"
echo "Magazine Solution - Database Configuration Setup"
echo "================================================"
echo ""

# Check if .env already exists
if [ -f .env ]; then
    echo "⚠️  .env file already exists!"
    read -p "Do you want to overwrite it? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Setup cancelled. Using existing .env file."
        exit 0
    fi
fi

# Copy template
echo "📋 Creating .env from template..."
cp .env.example .env

echo ""
echo "✅ .env file created!"
echo ""
echo "⚙️  Next steps:"
echo ""
echo "1. Edit .env and set your database password:"
echo "   nano .env"
echo "   (or use your preferred editor)"
echo ""
echo "2. Load environment variables:"
echo "   export \$(cat .env | xargs)"
echo ""
echo "3. Or use direnv (recommended):"
echo "   echo 'dotenv' > .envrc"
echo "   direnv allow"
echo ""
echo "4. Test the configuration:"
echo "   cd src/magazine-viewer"
echo "   dotnet run"
echo ""
echo "📚 For more information, see:"
echo "   - SETUP_DATABASE_CONFIG.md (quick guide)"
echo "   - DATABASE_CREDENTIALS_SOLUTION.md (full details)"
echo ""
echo "================================================"

