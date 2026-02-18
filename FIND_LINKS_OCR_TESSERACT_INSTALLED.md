# find-links OCR Issue - Tesseract Installed

## ✅ FIXED: Tesseract OCR is Now Installed

Tesseract OCR version 5.3.4 has been successfully installed on your system.

## Remaining Issue: MAGAZINE_IMAGE_ROOT Configuration

The OCR service is now functional, but you need to configure where your magazine images are stored.

### Current Situation

From the database, image paths are stored as relative paths like:
```
Magazines/Mayfair/Mayfair 06-10, 1971/1.jpg
```

The application needs to know the **root directory** where these images are located.

### Solution

You need to set the `MAGAZINE_IMAGE_ROOT` environment variable to point to the directory that contains the `Magazines` folder.

#### Option 1: Update Rider Run Configuration

Edit `.run/find-links.run.xml` and add the environment variable to the `<envs>` section:

```xml
<envs>
  <env name="MAGAZINE_DB" value="Host=localhost;Username=postgres;Password=Barnowl1;Database=magazines" />
  <env name="MAGAZINE_IMAGE_ROOT" value="/path/to/your/magazine/images" />
</envs>
```

Replace `/path/to/your/magazine/images` with the actual path where your magazine images are stored.

For example, if your images are at:
- `/mnt/magazine-data/Magazines/Mayfair/...` 
  Then set: `MAGAZINE_IMAGE_ROOT="/mnt/magazine-data"`

- `/home/justin/magazine-images/Magazines/Mayfair/...`
  Then set: `MAGAZINE_IMAGE_ROOT="/home/justin/magazine-images"`

#### Option 2: Set in Shell Profile

Add to your `~/.bashrc` or `~/.profile`:

```bash
export MAGAZINE_IMAGE_ROOT="/path/to/your/magazine/images"
```

Then restart your terminal or run `source ~/.bashrc`.

## How to Find Your Magazine Images

Run this command to search for the magazine images:

```bash
find / -type f -name "1.jpg" -path "*/Mayfair/Mayfair 06-10, 1971/*" 2>/dev/null
```

This will search your entire system for the image file. Once found, note the path and extract the root directory (everything before `Magazines/`).

## Testing the Fix

Once you've set `MAGAZINE_IMAGE_ROOT`, run find-links from Rider. You should see:

```
[DEBUG] Processing first page 1:
[DEBUG]   Image path from DB: Magazines/Mayfair/Mayfair 06-10, 1971/1.jpg
[DEBUG]   Full image path: /your/path/Magazines/Mayfair/Mayfair 06-10, 1971/1.jpg
[DEBUG]   File exists: True
[DEBUG]   OCR text length: 1234 characters
[DEBUG]   First 100 chars: [extracted text preview]
```

If `File exists: False`, then the path is wrong and you need to adjust `MAGAZINE_IMAGE_ROOT`.

## What Was Fixed

1. ✅ **Tesseract OCR installed** - version 5.3.4 with English language support
2. ✅ **Enhanced error messages** - TesseractOcrService now reports detailed errors
3. ✅ **Debug logging** - FindLinksApp shows diagnostic info for troubleshooting
4. ⚠️ **MAGAZINE_IMAGE_ROOT** - You need to configure this to point to your images

## Summary

**The OCR service is now working.** You just need to:
1. Find where your magazine images are stored
2. Set the `MAGAZINE_IMAGE_ROOT` environment variable to that location
3. Run find-links again

The application will now extract text from your magazine images using Tesseract OCR!

