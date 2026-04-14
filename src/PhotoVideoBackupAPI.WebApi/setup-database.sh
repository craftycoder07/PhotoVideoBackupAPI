#!/bin/bash

# PhotoVideoBackupAPI Database Setup Script
# This script helps set up PostgreSQL for the PhotoVideoBackupAPI

echo "Setting up PostgreSQL database for PhotoVideoBackupAPI..."

# Check if PostgreSQL is installed
if ! command -v psql &> /dev/null; then
    echo "PostgreSQL is not installed. Please install it first:"
    echo ""
    echo "On macOS with Homebrew:"
    echo "  brew install postgresql@15"
    echo "  brew services start postgresql@15"
    echo ""
    echo "On Ubuntu/Debian:"
    echo "  sudo apt update"
    echo "  sudo apt install postgresql postgresql-contrib"
    echo "  sudo systemctl start postgresql"
    echo "  sudo systemctl enable postgresql"
    echo ""
    echo "On Windows:"
    echo "  Download and install from https://www.postgresql.org/download/windows/"
    echo ""
    echo "After installing PostgreSQL, run this script again."
    exit 1
fi

# Check if PostgreSQL service is running
if ! pg_isready -q; then
    echo "PostgreSQL service is not running. Please start it:"
    echo ""
    echo "On macOS with Homebrew:"
    echo "  brew services start postgresql@15"
    echo ""
    echo "On Ubuntu/Debian:"
    echo "  sudo systemctl start postgresql"
    echo ""
    echo "On Windows:"
    echo "  Start PostgreSQL service from Services or use pgAdmin"
    echo ""
    exit 1
fi

echo "PostgreSQL is running. Creating databases..."

# Create databases
echo "Creating production database..."
createdb -U postgres PhotoVideoBackup 2>/dev/null || echo "Database PhotoVideoBackup might already exist"

echo "Creating development database..."
createdb -U postgres PhotoVideoBackup_Dev 2>/dev/null || echo "Database PhotoVideoBackup_Dev might already exist"

echo ""
echo "Database setup complete!"
echo ""
echo "Next steps:"
echo "1. Update the connection strings in appsettings.json and appsettings.Development.json if needed"
echo "2. Run the following command to apply database migrations:"
echo "   dotnet ef database update"
echo ""
echo "Default connection settings:"
echo "  Host: localhost"
echo "  Port: 5432"
echo "  Username: postgres"
echo "  Password: postgres"
echo "  Production DB: PhotoVideoBackup"
echo "  Development DB: PhotoVideoBackup_Dev"

