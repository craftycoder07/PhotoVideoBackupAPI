# Database Setup for PhotoVideoBackupAPI

This project now uses PostgreSQL for storing metadata while keeping media files on the local filesystem.

## Prerequisites

### Install PostgreSQL

**macOS (with Homebrew):**
```bash
brew install postgresql@15
brew services start postgresql@15
```

**Ubuntu/Debian:**
```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

**Windows:**
Download and install from [PostgreSQL Official Website](https://www.postgresql.org/download/windows/)

## Database Setup

### Option 1: Automated Setup (Recommended)
Run the provided setup script:
```bash
./setup-database.sh
```

### Option 2: Manual Setup

1. **Create databases:**
   ```bash
   # Production database
   createdb -U postgres PhotoVideoBackup
   
   # Development database
   createdb -U postgres PhotoVideoBackup_Dev
   ```

2. **Update connection strings** (if needed):
   - Edit `appsettings.json` for production settings
   - Edit `appsettings.Development.json` for development settings

## Apply Database Migrations

After setting up PostgreSQL, apply the Entity Framework migrations:

```bash
dotnet ef database update
```

This will create all the necessary tables and relationships in your database.

## Database Schema

The database includes the following main entities:

- **Devices**: Registered devices and their settings
- **BackupSessions**: Backup session information and progress
- **MediaItems**: Metadata for uploaded photos and videos

### Key Features:
- JSON columns for complex objects (Settings, Metadata, etc.)
- Proper foreign key relationships
- Indexes for performance
- Cascade delete for data integrity

## Connection Strings

Default connection strings are configured as:

**Production:**
```
Host=localhost;Database=PhotoVideoBackup;Username=******;Password=******;Port=5432
```

**Development:**
```
Host=localhost;Database=PhotoVideoBackup_Dev;Username=******;Password=******;Port=5432
```

## File Storage

Media files are still stored on the local filesystem in the configured `StorageSettings:BasePath` directory. The database only stores metadata about the files.

## Migration Commands

- **Create a new migration:** `dotnet ef migrations add MigrationName`
- **Update database:** `dotnet ef database update`
- **Remove last migration:** `dotnet ef migrations remove`

## Troubleshooting

### PostgreSQL Connection Issues
1. Ensure PostgreSQL service is running
2. Check connection string parameters
3. Verify database exists
4. Check firewall settings

### Migration Issues
1. Ensure all packages are restored: `dotnet restore`
2. Check for compilation errors: `dotnet build`
3. Verify connection string is correct

### Performance Tips
1. Consider adding indexes for frequently queried columns
2. Monitor database size and implement cleanup policies
3. Use connection pooling for high-traffic scenarios

