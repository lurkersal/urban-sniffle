using System;
using System.IO;
using IndexEditor.Shared;

class Program
{
    static int Main(string[] args)
    {
        // Usage: IndexSaverRunner [outputFolder]
        var outDir = args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0] : "/tmp/index_saved";
        try
        {
            var fullOut = Path.GetFullPath(outDir);
            Console.WriteLine($"IndexSaverRunner: saving _index.txt to folder: {fullOut}");
            Directory.CreateDirectory(fullOut);

            PopulateSampleEditorState();

            IndexSaver.SaveIndex(fullOut);

            var savedPath = Path.Combine(fullOut, "_index.txt");
            if (!File.Exists(savedPath))
            {
                Console.Error.WriteLine("Error: _index.txt was not created.");
                return 2;
            }

            Console.WriteLine($"Saved index to: {savedPath}\n");
            Console.WriteLine(File.ReadAllText(savedPath));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error writing _index.txt: " + ex.ToString());
            return 1;
        }
    }

    static void PopulateSampleEditorState()
    {
        // Provide a small, deterministic sample so the runner can be used for manual verification
        EditorState.Articles = new System.Collections.Generic.List<Common.Shared.ArticleLine>
        {
            new Common.Shared.ArticleLine
            {
                Pages = new System.Collections.Generic.List<int> { 77,78,108 },
                Category = "Feature",
                Title = "Courting St James's: how to belong to a gentleman's club",
                Authors = new System.Collections.Generic.List<string>{"Paul Keers"}
            },
            new Common.Shared.ArticleLine
            {
                Pages = new System.Collections.Generic.List<int> { 80,81,82,83,84,85 },
                Category = "Model",
                Title = "Louise",
                Authors = new System.Collections.Generic.List<string>{"Louise Cohen"}
            },
            new Common.Shared.ArticleLine
            {
                Pages = new System.Collections.Generic.List<int> { 87,89,91,92,93,94 },
                Category = "Fiction",
                Title = "Quest"
            }
        };

        EditorState.CurrentMagazine = "Mayfair";
        EditorState.CurrentVolume = "18";
        EditorState.CurrentNumber = "07";
    }
}
