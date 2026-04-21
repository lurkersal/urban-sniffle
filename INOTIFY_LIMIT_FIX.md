# Inotify Limit Error Fix

**Error**: `IOException: The configured user limit (128) on the number of inotify instances has been reached`

**Status**: ⚠️ **SYSTEM CONFIGURATION REQUIRED**

---

## Problem

Linux systems have a limit on the number of file watcher instances (inotify) that can be created. During development, ASP.NET Core uses file watchers for hot reload functionality. After many builds/restarts, you can hit this limit.

---

## Quick Fix (Temporary - Until Reboot)

Run these commands in your terminal:

```bash
# Increase inotify instances limit
sudo sysctl fs.inotify.max_user_instances=512

# Increase inotify watches limit (optional but recommended)
sudo sysctl fs.inotify.max_user_watches=524288
```

After running these commands, **restart your ASP.NET application**.

---

## Permanent Fix

To make these changes persist across reboots:

### Create/Edit sysctl Configuration

```bash
sudo nano /etc/sysctl.d/99-inotify.conf
```

### Add These Lines

```ini
fs.inotify.max_user_instances=512
fs.inotify.max_user_watches=524288
```

### Apply Changes

```bash
sudo sysctl -p /etc/sysctl.d/99-inotify.conf
```

---

## Alternative: Clean Up Watchers

If you don't want to increase limits, you can clean up existing watchers by stopping all dotnet processes:

```bash
# List dotnet processes
ps aux | grep dotnet

# Kill all dotnet processes
killall dotnet

# Or kill specific process by PID
kill <process_id>
```

Then restart your application.

---

## Why This Happens

1. **File Watchers**: ASP.NET Core watches files for changes (hot reload)
2. **Development Iterations**: Each build/restart creates new watchers
3. **Limit Reached**: After ~128 iterations, the system limit is hit
4. **Not Released**: Sometimes watchers aren't properly released

---

## Current Limits

Check your current limits:

```bash
cat /proc/sys/fs/inotify/max_user_instances
cat /proc/sys/fs/inotify/max_user_watches
```

Default on many systems:
- `max_user_instances`: 128
- `max_user_watches`: ~184,000

---

## Recommended Limits

For development machines:
- `max_user_instances`: **512** (or higher)
- `max_user_watches`: **524288** (or higher)

---

## After Fixing

1. Apply the sysctl changes (see above)
2. Stop the ASP.NET application if running
3. Restart the ASP.NET application
4. The error should be resolved

---

## Verify Fix

```bash
# Check new limits
cat /proc/sys/fs/inotify/max_user_instances
# Should show: 512

# Run the app
cd /home/justin/repos/urban-sniffle/the-archive
dotnet run --project src/TheArchive/TheArchive.csproj
```

---

## Summary

This is a **Linux system configuration issue**, not a code problem. Increase the inotify limits to allow more file watchers for development.

**Quick Fix Command**:
```bash
sudo sysctl fs.inotify.max_user_instances=512
```

Then restart your application.

---

**End of Fix Guide**

