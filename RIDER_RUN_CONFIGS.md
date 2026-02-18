# Rider Run Configurations Added

## What Was Done

I've created run configurations for your console applications. These will now appear in the Run configuration dropdown (top-right of Rider, next to the green Run button).

## Run Configurations Created

1. **find-links** - Configured with default argument "mayfair" and database connection
2. **file-renamer** - Ready to run with custom arguments
3. **magazine-parser** - Ready to run with custom arguments
4. **IndexEditor** - Already existed

## How to Use

### Switch Between Projects

1. Click the **dropdown** next to the Run button (currently shows "IndexEditor")
2. Select the project you want to run:
   - find-links
   - file-renamer
   - magazine-parser
   - IndexEditor

### Run the Selected Project

- Click the **green Run button** or press **Shift+F10**
- Or click the **green Debug button** or press **Shift+F9** to debug

### Modify Arguments

To change the command-line arguments for any project:

1. Select the configuration from the dropdown
2. Click **Edit Configurations...** (or the configuration name)
3. Modify the **Program arguments** field
4. Click **OK**

## Current find-links Configuration

- **Default argument:** `mayfair`
- **Environment variable:** `MAGAZINE_DB` is set to your PostgreSQL connection
- **Working directory:** `src/find-links`

To change the magazine type, edit the configuration and update the Program arguments field.

## Files Created

All run configurations are stored in:
```
/home/justin/repos/urban-sniffle/.run/
```

Files:
- `find-links.run.xml`
- `file-renamer.run.xml`
- `magazine-parser.run.xml`

These are version-controlled and will be shared with other developers if you commit them.

## Next Steps

**Restart Rider** or the configurations should appear automatically in the Run dropdown within a few seconds.

Then you can easily switch between running different projects!

