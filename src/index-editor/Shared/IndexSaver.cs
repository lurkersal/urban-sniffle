using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Shared
{
    public static class IndexSaver
    {
        // Save the current EditorState.Articles and metadata into _index.txt under folder.
        // Ensures the first non-comment line is an uncommented CSV metadata line: Magazine,Volume,Number
        public static void SaveIndex(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("folder is required", nameof(folder));
            var indexPath = Path.Combine(folder, "_index.txt");
            var lines = new List<string>();
            string Escape(string s) => s?.Replace(",", "\\,") ?? string.Empty;

            foreach (var a in EditorState.Articles ?? new List<Common.Shared.ArticleLine>())
            {
                var pagesText = a.PagesText ?? string.Empty;
                var modelNames = (a.ModelNames != null && a.ModelNames.Count > 0) ? string.Join('|', a.ModelNames) : string.Empty;
                var ages = (a.Ages != null && a.Ages.Count > 0) ? string.Join('|', a.Ages.Select(v => v.HasValue ? v.Value.ToString() : string.Empty)) : string.Empty;
                // Contributor column: prefer Authors if present (for Feature/Fiction/Humour etc.), otherwise use Photographers
                var contributor = (a.Contributors != null && a.Contributors.Count > 0) ? string.Join('|', a.Contributors) : string.Empty;
                var measurements = (a.Measurements != null && a.Measurements.Count > 0) ? string.Join('|', a.Measurements) : string.Empty;

                // Write the definitive 7-field format: pages,category,title,modelNames,ages,contributors,measurements
                var parts = new List<string> { pagesText, Escape(a.Category), Escape(a.Title), Escape(modelNames), Escape(ages), Escape(contributor), Escape(measurements) };
                lines.Add(string.Join(",", parts));
            }

            var outLinesList = new List<string>();
            if (!string.IsNullOrWhiteSpace(EditorState.CurrentMagazine) || !string.IsNullOrWhiteSpace(EditorState.CurrentVolume) || !string.IsNullOrWhiteSpace(EditorState.CurrentNumber))
            {
                var metaParts = new[] { Escape(EditorState.CurrentMagazine ?? string.Empty), Escape(EditorState.CurrentVolume ?? string.Empty), Escape(EditorState.CurrentNumber ?? string.Empty) };
                outLinesList.Add(string.Join(",", metaParts));
            }
            // Do not write legacy commented header lines; metadata is represented by the first CSV line only.

            outLinesList.AddRange(lines);

            var tempPath = indexPath + ".tmp";
            File.WriteAllLines(tempPath, outLinesList);
            if (File.Exists(indexPath))
            {
                File.Replace(tempPath, indexPath, null);
            }
            else
            {
                File.Move(tempPath, indexPath);
            }
        }
    }
}
