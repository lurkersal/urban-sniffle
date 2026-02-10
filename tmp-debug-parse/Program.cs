using System;
using IndexEditor.Shared;

class Program
{
    static void Main()
    {
        var s = "1-2|4,Feature,My Title,ModelA|ModelB,23|,John Doe|Jane Smith,Author One|Author Two,34-24-34";
        Console.WriteLine($"Line: {s}");
        try
        {
            var parts = IndexFileParser.SplitRespectingEscapedCommas(s);
            Console.WriteLine($"Parts ({parts.Count}):");
            for (int i = 0; i < parts.Count; i++) Console.WriteLine($"  [{i}]='{parts[i]}'");
            var art = IndexFileParser.ParseArticleLine(s);
            if (art == null) { Console.WriteLine("Parsed: null\n"); return; }
            Console.WriteLine($"Category: '{art.Category}'");
            Console.WriteLine($"Contributors: [{string.Join(",", art.Contributors ?? new System.Collections.Generic.List<string>())}]");
            Console.WriteLine($"Photographers: [{string.Join(",", art.Photographers)}]");
            Console.WriteLine($"Authors: [{string.Join(",", art.Authors)}]");
            Console.WriteLine($"Measurements: [{string.Join(",", art.Measurements)}]");
            Console.WriteLine();
        }
        catch (FormatException fx)
        {
            Console.WriteLine($"FormatException: {fx.Message}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EX: {ex.Message}\n");
        }
    }
}
