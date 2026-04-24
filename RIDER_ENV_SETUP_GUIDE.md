# Setting Up Magazine-Parser in Rider with Environment Variables

## Issue
When trying to configure magazine-parser in Rider, the "Select Path" dialog for .env file doesn't show the file or allow selection.

## Root Cause
The `.env` file didn't exist yet, and Rider's file browser can't select non-existent files.

## Solution

### ✅ Step 1: Create .env File (DONE)
The `.env` file has been created at `/home/justin/repos/urban-sniffle/.env`

### Step 2: Edit the .env File
Open `/home/justin/repos/urban-sniffle/.env` and update these values:

```bash
# Update this with your actual PostgreSQL credentials:
MAGAZINE_DB="Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YOUR_ACTUAL_PASSWORD"

# Update this with the path to your magazine images:
MAGAZINE_IMAGE_ROOT=/mnt/newvolume/path/to/your/magazines
```

### Step 3: Configure Rider Run Configuration

Now that the `.env` file exists, you can configure Rider:

#### Option A: Use EnvFile Plugin (Recommended)
1. In Rider, go to **Run → Edit Configurations**
2. Select your **magazine-parser** configuration
3. Find the **EnvFile** tab or section
4. Click **Add** or the **+** button
5. In the "Select Path" dialog:
   - **IMPORTANT**: Click the "eye" icon or "Show Hidden Files" button in the toolbar (top of dialog)
   - This makes `.env` files visible (they start with `.` so are hidden by default)
   - Navigate to `/home/justin/repos/urban-sniffle/`
   - **Double-click** the `urban-sniffle` folder to open it (don't just select it!)
   - You should now see `.env` in the file list
   - Select `.env` and click **OK**
6. Click **OK** to save the configuration

#### Option B: Set Environment Variables Directly
If the EnvFile plugin isn't working:

1. In Rider, go to **Run → Edit Configurations**
2. Select your **magazine-parser** configuration  
3. Find **Environment variables** field
4. Click the folder icon to open the editor
5. Add these variables manually:
   ```
   MAGAZINE_DB=Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=your_password
   MAGAZINE_IMAGE_ROOT=/mnt/newvolume/path/to/magazines
   ```
6. Click **OK**

### Step 4: Set Program Arguments
In the same run configuration:
- **Program arguments**: `--no-insert /path/to/magazine/folder`
  - Example: `--no-insert /home/justin/Magazines/Club\ International/Club\ International\ 17-01,\ 1988/`
- Or just: `--no-insert ./` if working directory is set correctly

### Step 5: Test the Configuration
1. Click **Run** (or Debug)
2. You should see:
   ```
   [ConnectionStringProvider] Loading from: Environment Variable
   Reading /path/to/_index.json...
   ```
3. If it works, the parser will read and validate the index file

## Required Environment Variables

### MAGAZINE_DB (Required)
**Purpose**: PostgreSQL database connection string

**Format**: 
```
Host=hostname;Port=5432;Database=dbname;Username=user;Password=password
```

**Example**:
```
MAGAZINE_DB="Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=mySecretPass123"
```

### MAGAZINE_IMAGE_ROOT (Required for importing)
**Purpose**: Root directory where magazine image files are stored

**Format**: Absolute path to directory

**Example**:
```
MAGAZINE_IMAGE_ROOT=/mnt/newvolume/Magazines
```

**Structure Expected**:
```
/mnt/newvolume/Magazines/
  ├── Club International/
  │   ├── Club International 17-01, 1988/
  │   │   ├── _index.json
  │   │   ├── 1.jpg
  │   │   ├── 2.jpg
  │   │   └── ...
  │   └── Club International 17-02, 1988/
  └── Mayfair/
      └── ...
```

## Troubleshooting

### .env File Not Visible in Rider's File Picker ⚠️
**This is the most common issue!**

**Cause**: Rider hides dotfiles (files starting with `.`) by default in the file picker dialog

**Solution**:
1. In the "Select Path" dialog, look at the **toolbar at the top**
2. Find and click the **"Show Hidden Files"** button (eye icon) or similar
3. The `.env` file should now appear in the file list
4. If still not visible, try:
   - Right-click in the file list → Show Hidden Files
   - Or press `Ctrl+H` (toggle hidden files)
   - Or use the gear/settings icon in the dialog

**Alternative**: Type the path directly
- In the "Select Path" dialog, find the path field at the top
- Type or paste: `/home/justin/repos/urban-sniffle/.env`
- Press Enter or click OK

### Cannot Select Folder (urban-sniffle shows as selected but can't proceed)
**Cause**: You're trying to select the folder instead of opening it

**Solution**:
1. **Double-click** the `urban-sniffle` folder to OPEN it (don't just click once to select)
2. You need to see the contents INSIDE the folder
3. Then find and select the `.env` file
4. Click OK

### File Not Visible in Rider
**Cause**: Hidden files not shown, or file doesn't exist

**Solution**:
1. Verify file exists: `ls -la /home/justin/repos/urban-sniffle/.env`
2. In Rider file browser, make sure "Show Hidden Files" is enabled
3. Refresh the project structure (Right-click → Reload from Disk)

### "Reflection-based serialization has been disabled"
**Cause**: JSON serialization issue (already fixed in code)

**Status**: ✅ Fixed in `IndexJsonSerializer.cs` by adding `TypeInfoResolver`

### Connection String Not Found
**Cause**: Environment variable not loaded

**Solutions**:
1. Check `.env` file exists and has correct values
2. Verify Rider loaded the EnvFile
3. Try Option B (set variables directly in run config)
4. Restart Rider after making changes

### Database Connection Fails
**Cause**: Wrong credentials or PostgreSQL not running

**Check**:
```bash
# Test connection
psql -h localhost -U postgres -d magazines

# Or using .env values:
source .env
psql "$MAGAZINE_DB"
```

## Alternative: System Environment Variables

Instead of using `.env`, you can set system-wide variables:

### Linux/Mac (Temporary - current session)
```bash
export MAGAZINE_DB="Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=yourpass"
export MAGAZINE_IMAGE_ROOT=/mnt/newvolume/Magazines
```

### Linux/Mac (Permanent - add to ~/.bashrc or ~/.zshrc)
```bash
echo 'export MAGAZINE_DB="Host=localhost;..."' >> ~/.bashrc
echo 'export MAGAZINE_IMAGE_ROOT=/mnt/newvolume/Magazines' >> ~/.bashrc
source ~/.bashrc
```

### Using direnv (Recommended for per-project config)
```bash
# Install direnv first
cd /home/justin/repos/urban-sniffle
echo 'dotenv' > .envrc
direnv allow
```

## Security Notes

⚠️ **IMPORTANT**: Never commit `.env` files to source control!

✅ The `.env` file should be in `.gitignore`
✅ Use `.env.example` as a template (safe to commit)
✅ Share `.env.example` with team, not `.env`

## Files Created/Modified

- ✅ `/home/justin/repos/urban-sniffle/.env` - Created from template
- ✅ Updated with `MAGAZINE_IMAGE_ROOT` variable
- ✅ Ready to be edited with your actual credentials

## Next Steps

1. **Edit `.env`** with your actual database password and image path
2. **Configure Rider** to use the `.env` file
3. **Test** by running magazine-parser in debug mode

---

**Status**: ✅ `.env` file created and ready for configuration  
**Action Required**: Update the file with your actual credentials and paths



