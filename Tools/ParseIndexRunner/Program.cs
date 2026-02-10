using System;
using IndexEditor.Shared;

class Program
{
    static int Main(string[] args)
    {
        string sample7 = "77-78|108,Humour,Funny piece,,,John Doe,"; // 7-col (authors absent) -- here John Doe is at photographers position
        string sample8 = "77-78|108,Humour,Funny piece,,,John Doe,Jane Author,"; // 8-col: photographers=John Doe, authors=Jane Author
        var lineArg = args.Length > 0 ? args[0] : null;
        var line = lineArg ?? sample8;
        try
        {
            if (lineArg != null && System.IO.File.Exists(lineArg))
            {
                var lines = System.IO.File.ReadAllLines(lineArg);
                int idx = 0;
                foreach (var raw in lines)
                {
                    idx++;
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    if (raw.TrimStart().StartsWith("#")) continue;
                    var debugParts = IndexFileParser.SplitRespectingEscapedCommas(raw);
                    var parsed = IndexFileParser.ParseArticleLine(raw);
                    Console.WriteLine($"Line {idx}: parts={debugParts.Count} -> Title='{(parsed?.Title ?? "<null>")}' Category='{(parsed?.Category ?? "<null>")}'");
                    if (parsed != null)
                    {
                        var contribCount = parsed.Contributors?.Count ?? 0;
                        Console.WriteLine($"  Contributors.count={contribCount} Contributor0='{parsed.Contributor0}' PagesText='{parsed.PagesText}'");
                    }
                }
                return 0;
            }
            else
            {
                // Debug: show raw parts as split by the parser
                var debugParts = IndexFileParser.SplitRespectingEscapedCommas(line);
                Console.WriteLine($"Debug: parts count = {debugParts.Count}");
                for (int i = 0; i < debugParts.Count; i++) Console.WriteLine($"  [{i}] '{debugParts[i]}'");

                var parsed = IndexFileParser.ParseArticleLine(line);
                if (parsed == null) { Console.WriteLine("parsed null"); return 2; }
                Console.WriteLine($"Parsed Title: {parsed.Title}");
                Console.WriteLine($"Parsed Category: {parsed.Category}");
                Console.WriteLine($"Contributors count: {parsed.Contributors?.Count}");
                if (parsed.Contributors != null) Console.WriteLine($"Contributors[0]: '{(parsed.Contributors.Count>0?parsed.Contributors[0]:"(none)")}'");
                Console.WriteLine($"Contributor0: '{parsed.Contributor0}'");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
            return 1;
        }
    }
}
