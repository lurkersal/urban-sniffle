# Database Configuration - Secure Setup Guide

## Problem
We need to avoid checking database credentials into source control while still allowing easy development setup in Rider/Visual Studio.

## Solution
The project uses a three-tier approach for database configuration:

### Priority Order (Highest to Lowest)
1. **Environment Variable**: `MAGAZINE_DB`
2. **User Secrets**: Configured via `dotnet user-secrets`
3. **Configuration Files**: `appsettings.json` (without credentials)

## Setup Options

### Option 1: Local Launch Settings (Recommended for Rider)

This is the easiest approach for Rider users.

1. **Copy the template file:**
   ```bash
   cd src/magazine-viewer/Properties
   cp launchSettings.local.json.template launchSettings.local.json
   ```

2. **Edit `launchSettings.local.json`** with your credentials:
   ```json
   {
     "profiles": {
       "http": {
         "environmentVariables": {
           "MAGAZINE_DB": "Host=localhost;Username=postgres;Password=YOUR_PASSWORD;Database=magazines",
           "MAGAZINE_IMAGE_ROOT": "/path/to/your/images"
         }
       }
     }
   }
   ```

3. **The file is gitignored** - Your credentials stay local and won't be committed.

**How Rider merges these files:**
- Rider automatically merges `launchSettings.json` and `launchSettings.local.json`
- Environment variables from the local file override the base file
- This allows each developer to have their own credentials

### Option 2: User Secrets (Cross-Platform)

User Secrets are stored outside the project directory and are never checked in.

1. **Initialize user secrets** (one-time setup):
   ```bash
   cd src/magazine-viewer
   dotnet user-secrets init
   ```

2. **Set the connection string:**
   ```bash
   dotnet user-secrets set "ConnectionStrings:MagazineDb" "Host=localhost;Username=postgres;Password=YOUR_PASSWORD;Database=magazines"
   ```

3. **Set the image root** (if needed):
   ```bash
   dotnet user-secrets set "MagazineImageRoot" "/path/to/your/images"
   ```

**Location of secrets:**
- Linux/macOS: `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`

### Option 3: System Environment Variable

Set a system-wide environment variable (survives reboots).

**Linux/macOS (add to `~/.bashrc` or `~/.zshrc`):**
```bash
export MAGAZINE_DB="Host=localhost;Username=postgres;Password=YOUR_PASSWORD;Database=magazines"
export MAGAZINE_IMAGE_ROOT="/path/to/your/images"
```

**Windows (PowerShell - Run as Administrator):**
```powershell
[System.Environment]::SetEnvironmentVariable('MAGAZINE_DB', 'Host=localhost;Username=postgres;Password=YOUR_PASSWORD;Database=magazines', 'User')
[System.Environment]::SetEnvironmentVariable('MAGAZINE_IMAGE_ROOT', 'C:\path\to\images', 'User')
```

**Rider Note:** After setting system environment variables, you need to restart Rider.

### Option 4: Rider Run Configuration (Per-Configuration)

1. In Rider, go to: **Run → Edit Configurations...**
2. Select your run configuration (e.g., "http" or "https")
3. Under **Environment Variables**, add:
   - `MAGAZINE_DB` = `Host=localhost;Username=postgres;Password=YOUR_PASSWORD;Database=magazines`
   - `MAGAZINE_IMAGE_ROOT` = `/path/to/your/images`
4. Click **OK**

**Note:** This is stored in `.idea/runConfigurations/` which should be gitignored.

## Other Projects

The same approach works for all console applications in the solution:

### find-links
Uses `MAGAZINE_DB` environment variable with hardcoded fallback in code.

### magazine-parser
Currently uses hardcoded connection string - should be updated to use `ConnectionStringProvider`.

### index-editor
Avalonia desktop app - can use any of the above methods.

## Security Best Practices

✅ **DO:**
- Use `launchSettings.local.json` for local development
- Use User Secrets for sensitive configuration
- Use environment variables for production deployments
- Keep `.gitignore` up to date

❌ **DON'T:**
- Commit `launchSettings.local.json`
- Put passwords in `launchSettings.json`
- Put passwords in `appsettings.json`
- Share your User Secrets ID or secrets.json file

## Verifying Your Setup

Run the magazine-viewer project. If you see an error like:
```
Database connection string not configured. Please set either:
  1. Environment variable: MAGAZINE_DB
  2. User secrets (dotnet user-secrets set "ConnectionStrings:MagazineDb" "<value>")
  3. appsettings.json: ConnectionStrings:MagazineDb
```

Then the configuration is not being loaded. Check that you've completed one of the setup options above.

## Team Setup

When a new developer joins:

1. Clone the repository
2. Follow one of the setup options above
3. The base `launchSettings.json` is checked in without credentials
4. Each developer creates their own `launchSettings.local.json` or uses User Secrets

## Migration from Hardcoded Credentials

If you find hardcoded credentials in code:

1. Replace with `ConnectionStringProvider.GetConnectionString()`
2. Add `using Common.Shared.Configuration;`
3. Remove the hardcoded connection string
4. Test that environment variable loading works

Example:
```csharp
// Before (BAD):
var connectionString = "Host=localhost;Username=postgres;Password=Barnowl1;Database=magazines";

// After (GOOD):
var connectionString = ConnectionStringProvider.GetConnectionString();
```

## Troubleshooting

### "Connection string not configured" error
- Verify environment variable is set: `echo $MAGAZINE_DB` (Linux/macOS) or `$env:MAGAZINE_DB` (PowerShell)
- Check User Secrets: `dotnet user-secrets list`
- Verify `launchSettings.local.json` exists and has correct format

### Rider not picking up environment variables
- Restart Rider after setting system environment variables
- Check that `launchSettings.local.json` is in the correct location
- Verify the file is valid JSON

### Still seeing hardcoded passwords in git
- Run: `git status` to see if `launchSettings.local.json` appears
- If it does, add it to `.gitignore` immediately
- Remove it from git: `git rm --cached src/magazine-viewer/Properties/launchSettings.local.json`

