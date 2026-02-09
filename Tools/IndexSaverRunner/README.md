IndexSaverRunner

A tiny helper program to exercise `IndexSaver.SaveIndex` and write a sample `_index.txt` for manual verification.

Usage

Build and run (default writes to `/tmp/index_saved`):

```bash
dotnet build Tools/IndexSaverRunner/IndexSaverRunner.csproj
dotnet run --project Tools/IndexSaverRunner/IndexSaverRunner.csproj -- [outputFolder]
```

If `outputFolder` is omitted the runner writes to `/tmp/index_saved`.

What it does

- Populates a small sample `EditorState` (articles + metadata).
- Calls `IndexSaver.SaveIndex(outputFolder)` to write `_index.txt`.
- Prints the path and contents of the written file.

This is intended for manual verification that `_index.txt` now starts with an uncommented CSV metadata line `Magazine,Volume,Number` followed by article rows.

