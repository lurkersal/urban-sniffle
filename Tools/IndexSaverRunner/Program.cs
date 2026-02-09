using System;
using IndexEditor.Shared;

class Program
{
    static void Main(string[] args)
    {
        var tmp = "/tmp/index_saved";
        try
        {
            System.IO.Directory.CreateDirectory(tmp);
            // Prepare sample EditorState
            EditorState.Articles = new System.Collections.Generic.List<Common.Shared.ArticleLine>
            {
                new Common.Shared.ArticleLine { Pages = new System.Collections.Generic.List<int> { 77,78,108 }, Category = "Feature", Title = "Courting St James's: how to belong to a gentleman's club", Authors = new System.Collections.Generic.List<string>{"Paul Keers"} },
                new Common.Shared.ArticleLine { Pages = new System.Collections.Generic.List<int> { 80,81,82,83,84,85 }, Category = "Model", Title = "Louise", Authors = new System.Collections.Generic.List<string>{"Louise Cohen"} },
                new Common.Shared.ArticleLine { Pages = new System.Collections.Generic.List<int> { 87,89,91,92,93,94 }, Category = "Fiction", Title = "Quest" }
            };
            EditorState.CurrentMagazine = "Mayfair";
            EditorState.CurrentVolume = "18-07";
            EditorState.CurrentNumber = "1983";

            IndexSaver.SaveIndex(tmp);
            Console.WriteLine("Saved index to: " + tmp + "/_index.txt");
            Console.WriteLine(System.IO.File.ReadAllText(System.IO.Path.Combine(tmp, "_index.txt")));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}

