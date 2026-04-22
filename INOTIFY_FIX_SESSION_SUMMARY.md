# inotify Limit Fix - Session Summary

**Date**: April 23, 2026  
**Status**: ✅ **FIXED AND RUNNING**

---

## Problem Encountered

```
IOException: The configured user limit (128) on the number of inotify 
instances has been reached
```

This error prevented the ASP.NET Core application from starting.

---

## Solution Applied

### 1. Increased inotify Limit
```bash
sudo sysctl fs.inotify.max_user_instances=512
```

**Result**: Limit changed from 128 → 512 (4x increase)

### 2. Made Change Permanent
```bash
echo "fs.inotify.max_user_instances=512" | sudo tee -a /etc/sysctl.conf
```

**Result**: Setting persists across reboots

---

## Verification

✅ **Limit verified**: 512 instances  
✅ **Application started**: PID 12474  
✅ **Server listening**: http://localhost:5163  
✅ **No errors**: Clean startup  

---

## Status

The application is now **running successfully** at:
- **URL**: http://localhost:5163
- **Process**: TheArchive (PID 12474)
- **Ports**: 5163 (IPv4 and IPv6)

---

## Why This Happened

Linux inotify instances are used by ASP.NET Core for:
- File change detection (hot reload)
- CSS/JavaScript watching
- Razor view monitoring

The default Linux limit (128) is too low for .NET development, especially after multiple app restarts that don't clean up file watchers.

---

## Prevention

The fix is **permanent** because it was added to `/etc/sysctl.conf`. You won't encounter this issue again unless you:
- Start hundreds of concurrent processes
- Never restart your machine (watchers accumulate)

If it happens again, you can:
1. Restart the machine (cleans up all watchers)
2. Kill dotnet processes: `killall dotnet`
3. Increase limit further: `sudo sysctl fs.inotify.max_user_instances=1024`

---

## Quick Reference

```bash
# Check current limit
cat /proc/sys/fs/inotify/max_user_instances

# Check usage
find /proc/*/fd -lname 'anon_inode:inotify' 2>/dev/null | wc -l

# Increase limit (temporary)
sudo sysctl fs.inotify.max_user_instances=512

# Increase limit (permanent)
echo "fs.inotify.max_user_instances=512" | sudo tee -a /etc/sysctl.conf
```

---

## Application Access

The Archive is now running and accessible at:
- **Local**: http://localhost:5163
- **IPv6**: http://[::1]:5163

You can now:
- Browse magazines
- View models
- See blurred backgrounds on issue/model pages
- Use page links dropdown
- Edit index files with Ctrl+I

---

**End of Session**

