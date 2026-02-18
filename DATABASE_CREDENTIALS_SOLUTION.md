# Database Credentials Security Solution

**Priority:** 🔴 CRITICAL  
**Effort:** 4 hours  
**Impact:** Eliminates security vulnerability

---

## Problem Statement

Database credentials are currently hardcoded in multiple locations:
- `src/magazine-parser/Program.cs:67`
- `src/magazine-viewer/Program.cs:9`
- `src/find-links/Program.cs:44` (partial - has fallback to env var)
- `scripts/restoredb.sh:8,11`

**Security Risk:** Credentials exposed in source control, visible to anyone with repository access.

---

## Solution Overview

Implement a multi-layered configuration approach:

1. **Environment Variables** (Production/CI/CD)
2. **User Secrets** (Development - ASP.NET Core)
3. **appsettings.json** (Non-sensitive defaults)
4. **Shared Configuration Helper** (Common library)

### Architecture

```
Application Startup
    ↓
ConnectionStringProvider.GetConnectionString()
    ↓
Check: 1. Environment Variable (MAGAZINE_DB)
       2. User Secrets (Development only)
       3. appsettings.json (localhost defaults, no password)
       4. Throw clear error if not found
```

---

## Implementation Steps

### Step 1: Create Shared Configuration Helper

**Location:** `src/common/Shared/Configuration/ConnectionStringProvider.cs`

```csharp
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Common.Shared.Configuration
{
    /// <summary>
    /// Centralized provider for database connection strings.
    /// Priority: Environment Variable > User Secrets > appsettings.json
    /// </summary>
    public static class ConnectionStringProvider
    {
        private const string ENV_VAR_NAME = "MAGAZINE_DB";
        private const string CONFIG_KEY = "ConnectionStrings:MagazineDb";
        
        /// <summary>
        /// Gets the database connection string from configuration.
        /// Throws InvalidOperationException if not configured.
        /// </summary>
        public static string GetConnectionString()
        {
            // Priority 1: Environment Variable
            var connString = Environment.GetEnvironmentVariable(ENV_VAR_NAME);
            if (!string.IsNullOrWhiteSpace(connString))
            {
                return connString;
            }
            
            // Priority 2 & 3: Configuration (User Secrets or appsettings.json)
            var configuration = BuildConfiguration();
            connString = configuration[CONFIG_KEY];
            
            if (!string.IsNullOrWhiteSpace(connString))
            {
                return connString;
            }
            
            // No configuration found
            throw new InvalidOperationException(
                $"Database connection string not configured. " +
                $"Please set either:\n" +
                $"  1. Environment variable: {ENV_VAR_NAME}\n" +
                $"  2. User secrets (dotnet user-secrets set \"{CONFIG_KEY}\" \"<value>\")\n" +
                $"  3. appsettings.json: {CONFIG_KEY}"
            );
        }
        
        /// <summary>
        /// Tries to get connection string. Returns false if not configured.
        /// </summary>
        public static bool TryGetConnectionString(out string? connectionString)
        {
            try
            {
                connectionString = GetConnectionString();
                return true;
            }
            catch
            {
                connectionString = null;
                return false;
            }
        }
        
        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder();
            
            // Add appsettings.json if it exists
            var settingsPath = FindAppsettingsJson();
            if (settingsPath != null)
            {
                builder.AddJsonFile(settingsPath, optional: true);
            }
            
            // Add user secrets in development
            if (IsDevelopmentEnvironment())
            {
                // User secrets are automatically loaded if configured
                builder.AddUserSecrets<ConnectionStringProviderMarker>(optional: true);
            }
            
            return builder.Build();
        }
        
        private static string? FindAppsettingsJson()
        {
            // Check current directory
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
                Path.Combine(AppContext.BaseDirectory ?? ".", "appsettings.json")
            };
            
            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }
            
            return null;
        }
        
        private static bool IsDevelopmentEnvironment()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                   ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            return env?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
    
    // Marker class for user secrets assembly attribute
    internal class ConnectionStringProviderMarker { }
}
```

### Step 2: Update common.csproj

Add required packages:

```xml
<ItemGroup>
  <PackageReference Include="Npgsql" Version="10.0.1" />
  <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="8.0.0" />
</ItemGroup>

<!-- Enable user secrets for development -->
<PropertyGroup>
  <UserSecretsId>magazine-solution-secrets-{GUID}</UserSecretsId>
</PropertyGroup>
```

### Step 3: Create appsettings.json Template

**File:** `src/common/appsettings.template.json`

```json
{
  "ConnectionStrings": {
    "MagazineDb": "Host=localhost;Port=5432;Database=magazines;Username=postgres"
  },
  "DatabaseSettings": {
    "DefaultHost": "localhost",
    "DefaultPort": 5432,
    "DefaultDatabase": "magazines",
    "DefaultUsername": "postgres"
  }
}
```

**Note:** Password is intentionally omitted - must be provided via user secrets or environment variable.

### Step 4: Update magazine-parser/Program.cs

**Before:**
```csharp
var connectionString = "Host=localhost;Username=postgres;Password=Barnowl1;Database=magazines";
```

**After:**
```csharp
using Common.Shared.Configuration;

// ...

var connectionString = ConnectionStringProvider.GetConnectionString();
```

### Step 5: Update magazine-viewer/Program.cs

**Before:**
```csharp
var connectionString = "Host=localhost;Username=postgres;Password=Barnowl1;Database=magazines";
builder.Services.AddSingleton(new MagazineDatabase(connectionString));
```

**After:**
```csharp
using Common.Shared.Configuration;

// ...

var connectionString = ConnectionStringProvider.GetConnectionString();
builder.Services.AddSingleton(new MagazineDatabase(connectionString));
```

### Step 6: Update find-links/Program.cs

**Before:**
```csharp
var connString = Environment.GetEnvironmentVariable("MAGAZINE_DB") 
    ?? "Host=localhost;Username=postgres;Password=Barnowl1;Database=magazines";
```

**After:**
```csharp
using Common.Shared.Configuration;

// ...

var connString = ConnectionStringProvider.GetConnectionString();
```

### Step 7: Update restoredb.sh Script

**Before:**
```bash
PGPASSWORD=Barnowl1 psql -U postgres -h localhost -d magazines -c "..."
```

**After:**
```bash
#!/bin/bash
# Restore the magazine database schema

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCHEMA_FILE="$SCRIPT_DIR/schema_postgres.sql"

# Check if password is provided via environment
if [ -z "$MAGAZINE_DB_PASSWORD" ]; then
    echo "Error: MAGAZINE_DB_PASSWORD environment variable not set"
    echo "Usage: MAGAZINE_DB_PASSWORD=your_password ./restoredb.sh"
    exit 1
fi

DB_HOST="${MAGAZINE_DB_HOST:-localhost}"
DB_USER="${MAGAZINE_DB_USER:-postgres}"
DB_NAME="${MAGAZINE_DB_NAME:-magazines}"

echo "Dropping and recreating schema on $DB_HOST..."
PGPASSWORD="$MAGAZINE_DB_PASSWORD" psql -U "$DB_USER" -h "$DB_HOST" -d "$DB_NAME" \
    -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"

echo "Restoring schema from $SCHEMA_FILE..."
PGPASSWORD="$MAGAZINE_DB_PASSWORD" psql -U "$DB_USER" -h "$DB_HOST" -d "$DB_NAME" \
    -f "$SCHEMA_FILE"

echo "Database restored successfully!"
```

### Step 8: Update .gitignore

Add to `.gitignore`:

```
# Environment files with secrets
.env
.env.local
.env.*.local
*.secret.*

# User secrets
secrets.json

# Configuration with credentials
appsettings.*.json
!appsettings.json
!appsettings.template.json
```

### Step 9: Create .env.example

**File:** `.env.example` (template for developers)

```bash
# Magazine Solution - Environment Variables Template
# Copy this file to .env and fill in your values
# DO NOT commit .env to source control

# Database Connection
MAGAZINE_DB="Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YOUR_PASSWORD_HERE"

# Alternative: Individual components (used by scripts)
MAGAZINE_DB_HOST=localhost
MAGAZINE_DB_PORT=5432
MAGAZINE_DB_NAME=magazines
MAGAZINE_DB_USER=postgres
MAGAZINE_DB_PASSWORD=YOUR_PASSWORD_HERE

# Environment
ASPNETCORE_ENVIRONMENT=Development
```

---

## Setup Instructions for Developers

### Option 1: Environment Variables (Recommended for Development)

1. Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and set your password

3. Load environment variables:
   ```bash
   # Linux/Mac (add to ~/.bashrc or ~/.zshrc)
   export $(cat .env | xargs)
   
   # Or use direnv (recommended)
   # Install: https://direnv.net/
   echo 'dotenv' > .envrc
   direnv allow
   ```

### Option 2: User Secrets (ASP.NET Core Projects)

For `magazine-viewer`:

```bash
cd src/magazine-viewer
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:MagazineDb" "Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YOUR_PASSWORD"
```

For other projects (using shared secrets):

```bash
cd src/common
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:MagazineDb" "Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YOUR_PASSWORD"
```

### Option 3: appsettings.json (Development Only - NOT RECOMMENDED)

Create `appsettings.Development.json` (gitignored):

```json
{
  "ConnectionStrings": {
    "MagazineDb": "Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

---

## Production Deployment

### CI/CD Pipeline (GitHub Actions Example)

```yaml
# .github/workflows/build.yml
env:
  MAGAZINE_DB: ${{ secrets.MAGAZINE_DB_CONNECTION }}
```

Set secret in GitHub: Settings > Secrets > Actions > New repository secret

### Docker Deployment

```dockerfile
# Dockerfile
ENV MAGAZINE_DB=${MAGAZINE_DB}
```

Run container:
```bash
docker run -e MAGAZINE_DB="Host=db;Port=5432;..." magazine-viewer
```

### Kubernetes

```yaml
# deployment.yaml
env:
  - name: MAGAZINE_DB
    valueFrom:
      secretKeyRef:
        name: magazine-secrets
        key: database-connection
```

Create secret:
```bash
kubectl create secret generic magazine-secrets \
  --from-literal=database-connection="Host=..."
```

### Azure App Service

Set in Configuration > Application Settings:
- Name: `MAGAZINE_DB`
- Value: `Host=...;Password=...`
- Deployment Slot Setting: Yes

---

## Testing the Solution

### Verify Configuration Loading

Create a test program:

```csharp
using Common.Shared.Configuration;

try
{
    var connString = ConnectionStringProvider.GetConnectionString();
    Console.WriteLine("✓ Connection string loaded successfully");
    
    // Don't print the full string (may contain password)
    var parts = connString.Split(';');
    Console.WriteLine($"  Components: {parts.Length}");
    Console.WriteLine($"  Contains Password: {connString.Contains("Password", StringComparison.OrdinalIgnoreCase)}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.Message}");
}
```

### Run All Projects

Test each project starts without hardcoded credentials:

```bash
# Test magazine-parser
cd src/magazine-parser
dotnet run -- test-folder

# Test magazine-viewer
cd src/magazine-viewer
dotnet run

# Test find-links
cd src/find-links
dotnet run -- Mayfair 21 12
```

---

## Security Checklist

- [ ] All hardcoded passwords removed from source code
- [ ] `.env` added to `.gitignore`
- [ ] `appsettings.*.json` gitignored (except templates)
- [ ] User secrets configured for development
- [ ] Environment variables documented in `.env.example`
- [ ] Production secrets configured in deployment platform
- [ ] Old commits with passwords handled (consider rotating password)
- [ ] Team notified of new setup process
- [ ] Documentation updated (README.md)

---

## Migration Checklist

### Phase 1: Preparation (30 min)
- [ ] Create `ConnectionStringProvider` class
- [ ] Update `common.csproj` with packages
- [ ] Create `.env.example`
- [ ] Update `.gitignore`

### Phase 2: Code Updates (1 hour)
- [ ] Update `magazine-parser/Program.cs`
- [ ] Update `magazine-viewer/Program.cs`
- [ ] Update `find-links/Program.cs`
- [ ] Update `scripts/restoredb.sh`

### Phase 3: Testing (1 hour)
- [ ] Configure development environment (user secrets or .env)
- [ ] Test magazine-parser
- [ ] Test magazine-viewer
- [ ] Test find-links
- [ ] Test restoredb.sh script

### Phase 4: Documentation (30 min)
- [ ] Update README.md with setup instructions
- [ ] Create developer onboarding guide
- [ ] Document CI/CD configuration

### Phase 5: Cleanup (1 hour)
- [ ] Git history review (consider BFG Repo-Cleaner if needed)
- [ ] Rotate database password
- [ ] Update production deployments
- [ ] Notify team members

---

## Benefits

✅ **Security:** No credentials in source control  
✅ **Flexibility:** Easy to change credentials per environment  
✅ **Consistency:** Single source of truth via `ConnectionStringProvider`  
✅ **Developer Experience:** Simple setup with user secrets or .env  
✅ **Production Ready:** Environment variable support for all platforms  
✅ **Testability:** Can inject test connection strings  

---

## Additional Recommendations

### 1. Consider Azure Key Vault / AWS Secrets Manager

For production, consider using cloud secret management:

```csharp
// Add Microsoft.Extensions.Configuration.AzureKeyVault
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### 2. Connection String Validation

Add validation to `ConnectionStringProvider`:

```csharp
private static void ValidateConnectionString(string connectionString)
{
    if (!connectionString.Contains("Host", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Connection string missing 'Host'");
        
    if (!connectionString.Contains("Database", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Connection string missing 'Database'");
        
    // Don't require password in dev (could use peer auth)
}
```

### 3. Configuration Encryption

For `appsettings.json` passwords (not recommended but if needed):

```bash
# ASP.NET Core Data Protection
dotnet user-secrets set "ConnectionStrings:MagazineDb:Password" "encrypted_value"
```

### 4. Audit Logging

Log configuration source (without credentials):

```csharp
public static string GetConnectionString()
{
    var source = DetermineSource();
    Console.WriteLine($"Loading connection string from: {source}");
    // ... rest of implementation
}
```

---

## References

- [ASP.NET Core User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [.NET Configuration](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [Environment Variables Best Practices](https://12factor.net/config)
- [Git Secret Scanning](https://docs.github.com/en/code-security/secret-scanning)

---

**Implementation Date:** TBD  
**Assigned To:** Development Team  
**Review Required:** Yes - Security team review recommended

