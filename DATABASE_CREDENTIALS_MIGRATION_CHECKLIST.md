# Database Credentials Migration - Implementation Checklist

**Status:** Ready for Implementation  
**Priority:** 🔴 CRITICAL - Security Issue  
**Estimated Time:** 4 hours  

---

## ✅ Completed (Already Done)

- [x] Created `ConnectionStringProvider` class
- [x] Updated `common.csproj` with required packages
- [x] Created `.env.example` template
- [x] Updated `.gitignore` to exclude sensitive files
- [x] Created `appsettings.template.json`
- [x] Updated `magazine-parser/Program.cs`
- [x] Updated `magazine-viewer/Program.cs`
- [x] Updated `find-links/Program.cs`
- [x] Updated `scripts/restoredb.sh`
- [x] Created setup documentation

---

## 📋 TODO - Complete the Migration

### Phase 1: Build and Test (30 minutes)

- [ ] **Restore NuGet packages**
  ```bash
  cd /home/justin/repos/urban-sniffle
  dotnet restore
  ```

- [ ] **Build the solution**
  ```bash
  dotnet build
  ```
  
- [ ] **Check for compilation errors**
  - Fix any namespace issues
  - Verify all projects reference common correctly

### Phase 2: Configure Development Environment (15 minutes)

Choose **ONE** of these options:

#### Option A: Environment Variable (Recommended)
- [ ] Copy `.env.example` to `.env`
  ```bash
  cp .env.example .env
  ```
  
- [ ] Edit `.env` and set your actual password
  ```bash
  nano .env
  # Change YOUR_PASSWORD_HERE to your actual password
  ```
  
- [ ] Load environment variables
  ```bash
  export $(cat .env | xargs)
  ```
  
- [ ] Verify it loaded
  ```bash
  echo $MAGAZINE_DB
  # Should show your connection string
  ```

#### Option B: User Secrets
- [ ] Set user secret for magazine-viewer
  ```bash
  cd src/magazine-viewer
  dotnet user-secrets set "ConnectionStrings:MagazineDb" "Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YourPassword"
  ```
  
- [ ] Set user secret for common (used by console apps)
  ```bash
  cd src/common
  dotnet user-secrets set "ConnectionStrings:MagazineDb" "Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YourPassword"
  ```

### Phase 3: Testing (1 hour)

- [ ] **Test magazine-viewer**
  ```bash
  cd src/magazine-viewer
  dotnet run
  ```
  - [ ] Opens without errors
  - [ ] Can access database
  - [ ] No hardcoded credentials in console output

- [ ] **Test magazine-parser**
  ```bash
  cd src/magazine-parser
  dotnet run -- /path/to/test/folder
  ```
  - [ ] Runs without connection errors
  - [ ] Loads categories from database
  - [ ] Shows connection source in output

- [ ] **Test find-links**
  ```bash
  cd src/find-links
  dotnet run -- Mayfair 21 12
  ```
  - [ ] Connects to database successfully
  - [ ] No hardcoded credentials visible

- [ ] **Test index-editor**
  ```bash
  cd src/index-editor
  dotnet run
  ```
  - [ ] Application starts
  - [ ] Can load categories (if used)

- [ ] **Test restoredb.sh**
  ```bash
  export MAGAZINE_DB_PASSWORD=YourPassword
  ./scripts/restoredb.sh
  ```
  - [ ] Shows error if password not set (good!)
  - [ ] Works when password is provided
  - [ ] Database restores successfully

### Phase 4: Code Review (30 minutes)

- [ ] **Verify no hardcoded credentials remain**
  ```bash
  cd /home/justin/repos/urban-sniffle
  grep -r "Password=Barnowl1" --include="*.cs" --include="*.sh"
  # Should return NO results
  ```

- [ ] **Verify .env is gitignored**
  ```bash
  git status
  # .env should NOT appear in untracked files
  ```

- [ ] **Verify ConnectionStringProvider is used everywhere**
  ```bash
  grep -r "new NpgsqlConnection" --include="*.cs" | grep -v ConnectionStringProvider
  # Review any results
  ```

### Phase 5: Documentation (1 hour)

- [ ] **Update main README.md**
  - [ ] Add section "Database Configuration"
  - [ ] Link to `SETUP_DATABASE_CONFIG.md`
  - [ ] Note that setup is required

- [ ] **Create developer onboarding guide** (optional)
  - [ ] List all setup steps
  - [ ] Include database configuration
  - [ ] Add troubleshooting section

- [ ] **Notify team members**
  - [ ] Send email/message about changes
  - [ ] Share setup instructions
  - [ ] Offer to help with setup

### Phase 6: Git Commit (15 minutes)

- [ ] **Review changes**
  ```bash
  git status
  git diff
  ```

- [ ] **Stage files**
  ```bash
  git add .
  ```

- [ ] **Verify .env is NOT staged**
  ```bash
  git status
  # .env should NOT appear
  ```

- [ ] **Commit changes**
  ```bash
  git commit -m "Security: Remove hardcoded database credentials

  - Add ConnectionStringProvider for centralized config
  - Support environment variables, user secrets, appsettings
  - Update all projects to use secure configuration
  - Update .gitignore to exclude .env and secrets
  - Add setup documentation

  BREAKING CHANGE: Database password must now be configured
  via environment variable MAGAZINE_DB or user secrets.
  See SETUP_DATABASE_CONFIG.md for setup instructions.
  "
  ```

- [ ] **Push to repository**
  ```bash
  git push
  ```

### Phase 7: Security Cleanup (30 minutes)

**⚠️ IMPORTANT:** The old password is now in git history!

#### Option A: Git History Cleanup (Recommended for Private Repos)

- [ ] **Use BFG Repo-Cleaner to remove password from history**
  ```bash
  # Install BFG
  # https://rtyley.github.io/bfg-repo-cleaner/
  
  # Clone a fresh copy
  git clone --mirror https://your-repo-url magazine.git
  cd magazine.git
  
  # Remove passwords from all commits
  bfg --replace-text passwords.txt
  
  # Clean up
  git reflog expire --expire=now --all
  git gc --prune=now --aggressive
  
  # Force push (⚠️ requires team coordination)
  git push --force
  ```

#### Option B: Rotate Database Password (Easier)

- [ ] **Change PostgreSQL password**
  ```bash
  psql -U postgres
  ALTER USER postgres WITH PASSWORD 'new_secure_password';
  ```

- [ ] **Update your local configuration**
  ```bash
  # Update .env or user secrets with new password
  ```

- [ ] **Notify team to update their local configs**

#### Option C: Accept Risk (Small Teams/Personal Projects)

- [ ] **Document that old password is in git history**
- [ ] **Ensure database is not publicly accessible**
- [ ] **Monitor for unauthorized access**

### Phase 8: CI/CD Configuration (if applicable)

- [ ] **GitHub Actions**
  - [ ] Add `MAGAZINE_DB_CONNECTION` to repository secrets
  - [ ] Update workflows to use secret

- [ ] **Docker**
  - [ ] Update docker-compose.yml with environment variable
  - [ ] Document how to pass connection string

- [ ] **Production Deployment**
  - [ ] Configure connection string in hosting platform
  - [ ] Test deployment with new configuration

---

## 🎯 Success Criteria

All checkboxes must be complete AND:

- ✅ All projects build without errors
- ✅ All projects run without hardcoded credentials
- ✅ Tests pass (at least the existing 31 tests)
- ✅ No credentials in source code (verified with grep)
- ✅ `.env` file is gitignored
- ✅ Documentation is updated
- ✅ Team is notified
- ✅ Old password is rotated or history cleaned

---

## 📊 Progress Tracking

| Phase | Status | Time | Notes |
|-------|--------|------|-------|
| Files Created | ✅ Complete | - | All implementation files ready |
| Build & Test | ⏳ Pending | 30 min | |
| Dev Environment | ⏳ Pending | 15 min | |
| Testing | ⏳ Pending | 60 min | |
| Code Review | ⏳ Pending | 30 min | |
| Documentation | ⏳ Pending | 60 min | |
| Git Commit | ⏳ Pending | 15 min | |
| Security Cleanup | ⏳ Pending | 30 min | |
| CI/CD Config | ⏳ Pending | 30 min | |

**Total Estimated Time:** 4 hours

---

## 🆘 Troubleshooting

### Build Errors

**Error:** `The type or namespace name 'Configuration' does not exist`

**Solution:** Restore packages
```bash
dotnet restore
dotnet build
```

### Runtime Errors

**Error:** `Connection string not configured`

**Solution:** Set up environment variable or user secrets (see Phase 2)

### Database Connection Fails

**Error:** `Password authentication failed`

**Solution:** Verify password in configuration matches PostgreSQL password

---

## 📝 Notes

- Take breaks between phases
- Test thoroughly before committing
- Keep `.env.example` updated if connection string format changes
- Document any issues encountered for future reference

---

**Start Date:** ________________  
**Completed Date:** ________________  
**Completed By:** ________________

