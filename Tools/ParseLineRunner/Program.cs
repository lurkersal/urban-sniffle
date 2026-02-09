using System;
using IndexEditor;

class Program
{
    static int Main(string[] args)
    {
        var line = "77-78|108,Humour,Funny piece,,,John Doe,"; // sample 7-column where authors field is at index 6 for 8-col format
        if (args.Length > 0) line = args[0];
        try
        {
            var mw = new MainWindow();
            var parsed = mw.GetType().GetMethod("ParseArticleLine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(mw, new object[] { line }) as Common.Shared.ArticleLine;
            if (parsed == null) { Console.WriteLine("parsed null"); return 2; }
            Console.WriteLine($"Title: {parsed.Title}");
            Console.WriteLine($"Category: {parsed.Category}");
            Console.WriteLine($"Authors count: {parsed.Authors?.Count}");
            if (parsed.Authors != null) Console.WriteLine($"Authors[0]: { (parsed.Authors.Count>0?parsed.Authors[0]:"(none)") }");
            Console.WriteLine($"Author0 property: {parsed.Author0}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error invoking parser: " + ex);
            return 1;
        }
    }
}

