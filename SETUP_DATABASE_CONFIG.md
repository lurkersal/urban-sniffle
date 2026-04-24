# Database Configuration Setup Guide

**Quick Start:** Get the Magazine solution running with secure database credentials

---

## 🚀 Quick Setup (5 minutes)

### Option 1: Environment Variable (Recommended)

1. **Copy the example file:**
   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` and set your password:**
   ```bash
   nano .env  # or use your preferred editor
   ```
   
   Change:
   ```
   MAGAZINE_DB="Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YOUR_PASSWORD_HERE"
   ```
   
   To:
   ```
   MAGAZINE_DB="Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YourActualPassword"
   ```

3. **Load the environment variables:**
   
   **Linux/Mac:**
   ```bash
   export $(cat .env | xargs)
   ```
   
   **Or add to your shell profile (`~/.bashrc`, `~/.zshrc`, etc.):**
   ```bash
   export MAGAZINE_DB="Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YourPassword"
   ```

4. **Verify it worked:**
   ```bash
   echo $MAGAZINE_DB
   # Should print your connection string
   ```

5. **Run the applications:**
   ```bash
   # Test magazine-viewer
   cd src/magazine-viewer
   dotnet run
   
   # Test magazine-parser
   cd src/magazine-parser
   dotnet run -- /path/to/magazine/folder
   ```

### Option 2: User Secrets (ASP.NET Core - Development Only)

**For magazine-viewer:**
```bash
cd src/magazine-viewer
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:MagazineDb" "Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YourPassword"
```

**For console apps (using common project):**
```bash
cd src/common
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:MagazineDb" "Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YourPassword"
```

**Verify:**
```bash
dotnet user-secrets list
```

---

## 🔐 For Scripts

When running the database restore script:

```bash
# Set the password environment variable
export MAGAZINE_DB_PASSWORD=YourPassword

# Run the script
./scripts/restoredb.sh
```

Or in one line:
```bash
MAGAZINE_DB_PASSWORD=YourPassword ./scripts/restoredb.sh
```

---

## 🐳 Using direnv (Recommended for Teams)

[direnv](https://direnv.net/) automatically loads `.env` files when you enter the directory.

1. **Install direnv:**
   ```bash
   # Ubuntu/Debian
   sudo apt install direnv
   
   # Mac
   brew install direnv
   ```

2. **Add to your shell (~/.bashrc or ~/.zshrc):**
   ```bash
   eval "$(direnv hook bash)"  # or zsh, fish, etc.
   ```

3. **Create .envrc in project root:**
   ```bash
   echo 'dotenv' > .envrc
   direnv allow
   ```

4. **Done!** Environment variables load automatically when you `cd` into the project.

---

## ✅ Testing Your Setup

Run this test to verify configuration is working:

```bash
cd src/magazine-viewer
dotnet run
```

You should see:
```
[ConnectionStringProvider] Loading from: Environment Variable
```

Or:
```
[ConnectionStringProvider] Loading from: Configuration (User Secrets or appsettings.json)
```

**If you see an error** about missing connection string, go back and complete the setup.

---

## 🚨 Common Issues

### "Connection string not configured"

**Cause:** No configuration method is set up.

**Solution:** Choose Option 1 or Option 2 above and complete all steps.

### "Password authentication failed"

**Cause:** Wrong password in configuration.

**Solution:** 
1. Verify PostgreSQL password: `psql -U postgres -h localhost -d magazines`
2. Update your configuration with the correct password

### Environment variable not loading

**Cause:** Shell didn't reload environment.

**Solution:**
```bash
# Reload your shell
source ~/.bashrc  # or ~/.zshrc

# Or start a new terminal session
```

### "User secrets not found"

**Cause:** User secrets not initialized for the project.

**Solution:**
```bash
cd src/magazine-viewer  # or appropriate project
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:MagazineDb" "your-connection-string"
```

---

## 🏭 Production Deployment

### GitHub Actions

Add to repository secrets (Settings > Secrets > Actions):

- Name: `MAGAZINE_DB_CONNECTION`
- Value: `Host=prod-server;Port=5432;Database=magazines;Username=app_user;Password=SecurePassword`

In workflow:
```yaml
env:
  MAGAZINE_DB: ${{ secrets.MAGAZINE_DB_CONNECTION }}
```

### Docker

```dockerfile
ENV MAGAZINE_DB=${MAGAZINE_DB}
```

Run:
```bash
docker run -e MAGAZINE_DB="Host=db;Port=5432;..." magazine-app
```

### Kubernetes

```yaml
env:
  - name: MAGAZINE_DB
    valueFrom:
      secretKeyRef:
        name: magazine-secrets
        key: database-connection
```

---

## 📝 Next Steps

After setup is complete:

1. ✅ Verify all projects run successfully
2. ✅ Commit your changes (`.env` is gitignored - safe!)
3. ✅ Update team members about new setup process
4. 🔒 Consider rotating the database password (old one was in git history)

---

## 🆘 Need Help?

1. Check the detailed guide: `DATABASE_CREDENTIALS_SOLUTION.md`
2. Verify `.env` file exists and is not committed
3. Test connection manually: `psql -U postgres -h localhost -d magazines`
4. Check environment variables are loaded: `echo $MAGAZINE_DB`

---

**Security Note:** Never commit `.env` files or files containing passwords to git!

