using System;
using Common.Shared;

class Program
{
    static int Main(string[] args)
    {
        string[] valids = new[] { "36B-28-38", "36-28-38", "34C-22-34", "36DD-28-38", "34C–22–34", "34C - 22 - 34" };
        string[] invalids = new[] { "", "36B/28/38", "36B-28cm-38", "36B-28", "5-4-3", "36.5B-28-38" };

        Console.WriteLine("Valid inputs:");
        foreach (var s in valids)
        {
            var ok = MeasurementsValidator.TryParseMeasurements(s, out var bust, out var cup, out var waist, out var hip, out var err);
            Console.WriteLine($"{s} => OK={ok}, bust={bust}, cup={cup}, waist={waist}, hip={hip}, err={err}");
        }
        Console.WriteLine();
        Console.WriteLine("Invalid inputs:");
        foreach (var s in invalids)
        {
            var ok = MeasurementsValidator.TryParseMeasurements(s, out var bust, out var cup, out var waist, out var hip, out var err);
            Console.WriteLine($"{s} => OK={ok}, err={err}");
        }

        return 0;
    }
}
