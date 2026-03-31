# The Archive - Quick Start Guide

## ⚠️ "Address Already In Use" Error

If you see this error:
```
System.IO.IOException: Failed to bind to address http://127.0.0.1:5163: address already in use.
```

It means TheArchive is already running. Here are your options:

---

## 🔧 Solution Options

### Option 1: Use the Stop Script (Recommended)
```bash
cd the-archive
./stop-archive.sh
```

Then start normally:
```bash
./start-archive.sh
```

### Option 2: One-Line Kill Command
```bash
lsof -ti:5163 | xargs kill -9 2>/dev/null && echo "✅ Port freed!"
```

### Option 3: Manual Kill
```bash
# Find the process
lsof -ti:5163

# Kill it (replace XXXX with the PID shown)
kill -9 XXXX
```

---

## 🚀 Easy Start/Stop Commands

### Start TheArchive
```bash
cd /home/justin/repos/urban-sniffle/the-archive
./start-archive.sh
```
This script automatically stops any running instance first!

### Stop TheArchive
```bash
cd /home/justin/repos/urban-sniffle/the-archive
./stop-archive.sh
```

### Quick Kill
```bash
pkill -9 -f "TheArchive"
```

---

## 📝 Common Scenarios

### Scenario 1: Started in background and forgot
**Solution:**
```bash
./stop-archive.sh
```

### Scenario 2: Terminal closed but app still running
**Solution:**
```bash
./stop-archive.sh
```

### Scenario 3: Want to restart after code changes
**Solution:**
```bash
./start-archive.sh  # This auto-stops old version first!
```

---

## 🎯 Best Practices

1. **Always use `./start-archive.sh`** - It handles cleanup automatically
2. **Press Ctrl+C to stop** when running in foreground
3. **Use `./stop-archive.sh`** if you need to clean up
4. **Don't run `dotnet run` in background** without tracking the PID

---

## 🔍 Check If Running

```bash
# Check if port is in use
lsof -ti:5163

# Check for TheArchive processes
pgrep -af TheArchive

# Test if app responds
curl -s http://localhost:5163/test/db
```

---

## 📍 Application URLs

- Homepage: http://localhost:5163/
- Issues: http://localhost:5163/issues
- Articles: http://localhost:5163/articles
- Models: http://localhost:5163/models
- Photographers: http://localhost:5163/photographers
- Database Test: http://localhost:5163/test/db

---

## 🆘 Still Having Issues?

Try the nuclear option:
```bash
# Kill ALL dotnet processes (use with caution!)
pkill -9 dotnet

# Then wait a moment
sleep 2

# Now start fresh
./start-archive.sh
```

---

## 📁 Script Locations

- Start Script: `the-archive/start-archive.sh`
- Stop Script: `the-archive/stop-archive.sh`

Both are executable and ready to use!

