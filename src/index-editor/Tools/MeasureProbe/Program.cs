using System;
using IndexEditor.Shared;

namespace IndexEditor.Tools.MeasureProbe
{
    // Helper: non-entry-point probe function. Renamed so it won't be treated as an application entry point
    // if this file is accidentally compiled into the main IndexEditor assembly.
    internal static class MeasureProbeHelper
    {
        public static void RunProbe()
        {
            var lines = new[] {
                "80-85,Model,Louise,Louise Cohen,23,John Allum,35C-23-36",
                "5,Feature,Title,ModelName,23,Photographer,Author,36B-28-38",
                "77-78|108,Feature,Courting St James's: how to belong to a gentleman's club,,,Paul Keers,"
            };

            foreach (var line in lines)
            {
                var parsed = IndexFileParser.ParseArticleLine(line);
                Console.WriteLine($"Line: {line}");
                if (parsed == null) { Console.WriteLine("  Parsed: null"); continue; }
                Console.WriteLine($"  Category: {parsed.Category}");
                Console.WriteLine($"  Title: {parsed.Title}");
                Console.WriteLine($"  Measurements count: {parsed.Measurements?.Count ?? 0}");
                if (parsed.Measurements != null)
                    Console.WriteLine($"  Measurements[0]: '{string.Join(";", parsed.Measurements)}'");
                Console.WriteLine();
            }
        }
    }
}
