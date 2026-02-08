using System;
using System.Collections.Generic;
using Common.Shared;
using IndexEditor.Shared;

namespace IndexEditor.Tools
{
    public static class DemoRunner
    {
        public static void Run()
        {
            Console.WriteLine("Interactive Demo: segment lifecycle (add -> cancel -> reopen -> end)");

            // Setup two articles
            var a1 = new ArticleLine { Title = "Article A", Category = "Feature" };
            a1.Pages = new List<int> { 2, 3 };
            var a2 = new ArticleLine { Title = "Article B", Category = "Fiction" };
            a2.Pages = new List<int> { 10 };

            EditorState.Articles = new List<ArticleLine> { a1, a2 };
            EditorState.ActiveArticle = a1;
            EditorState.CurrentPage = 5;

            PrintState("Initial state");

            // Add segment on a1 at page 5
            Console.WriteLine("\n-- Add Segment at current page (5) --");
            var seg = new Segment(5);
            seg.WasNew = true;
            a1.Segments.Add(seg);
            EditorState.ActiveSegment = seg;
            EditorState.NotifyStateChanged();
            PrintState("After AddSegment");

            // Cancel segment -> should remove because WasNew
            Console.WriteLine("\n-- Cancel Segment (should remove new segment) --");
            if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
            {
                var s = EditorState.ActiveSegment;
                var art = EditorState.ActiveArticle;
                if (s.WasNew)
                {
                    art.Segments.Remove(s);
                    Console.WriteLine("Removed newly created segment from article");
                }
                else if (s.OriginalEnd.HasValue)
                {
                    s.End = s.OriginalEnd;
                    s.OriginalEnd = null;
                    Console.WriteLine("Restored original end on reopened segment");
                }
                else
                {
                    s.End = s.Start;
                    Console.WriteLine("Closed segment to single page (fallback)");
                }
                s.WasNew = false;
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            PrintState("After Cancel");

            // Create a closed segment and then reopen it (simulate clicking it)
            Console.WriteLine("\n-- Create closed segment 7-8 then reopen it --");
            var closed = new Segment(7, 8);
            a1.Segments.Add(closed);
            PrintState("After adding closed segment 7-8");

            Console.WriteLine("Reopening closed segment (should set OriginalEnd and clear End)");
            if (closed.End.HasValue)
            {
                closed.OriginalEnd = closed.End;
                closed.WasNew = false;
                closed.End = null; // reopen
                EditorState.ActiveSegment = closed;
                EditorState.CurrentPage = closed.Start;
                EditorState.NotifyStateChanged();
            }
            PrintState("After Reopen");

            // Cancel reopen -> should restore original end
            Console.WriteLine("\n-- Cancel reopened segment (should restore 7-8) --");
            if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
            {
                var s = EditorState.ActiveSegment;
                if (s.WasNew)
                {
                    var art = EditorState.ActiveArticle;
                    art.Segments.Remove(s);
                    Console.WriteLine("Removed new segment");
                }
                else if (s.OriginalEnd.HasValue)
                {
                    s.End = s.OriginalEnd;
                    s.OriginalEnd = null;
                    Console.WriteLine($"Restored original end {s.End}");
                }
                else
                {
                    s.End = s.Start;
                    Console.WriteLine("Closed segment fallback");
                }
                s.WasNew = false;
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            PrintState("After Cancel Reopen");

            // Now reopen again and End the segment
            Console.WriteLine("\n-- Reopen and End segment (should add pages to article) --");
            closed.OriginalEnd = closed.End;
            closed.End = null;
            closed.WasNew = false;
            EditorState.ActiveSegment = closed;
            EditorState.CurrentPage = 9; // end at 9
            EditorState.NotifyStateChanged();
            PrintState("After Reopen before End");

            // End
            if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
            {
                var start = EditorState.ActiveSegment.Start;
                var end = EditorState.CurrentPage;
                if (end < start) (start, end) = (end, start);
                var art = EditorState.ActiveArticle;
                if (art != null)
                {
                    var newPages = new List<int>(art.Pages ?? new List<int>());
                    for (int p = start; p <= end; p++) if (!newPages.Contains(p)) newPages.Add(p);
                    newPages.Sort();
                    art.Pages = newPages;
                }
                EditorState.ActiveSegment.End = EditorState.CurrentPage;
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            PrintState("After End Segment");

            Console.WriteLine("\nDemo complete.");
        }

        static void PrintState(string label)
        {
            Console.WriteLine($"\n== {label} ==");
            Console.WriteLine($"CurrentPage: {EditorState.CurrentPage}");
            Console.WriteLine($"ActiveArticle: {EditorState.ActiveArticle?.Title ?? "(none)"}");
            Console.WriteLine($"ActiveSegment: {(EditorState.ActiveSegment == null ? "(none)" : EditorState.ActiveSegment.Display)}");
            Console.WriteLine("Articles and segments:");
            foreach (var a in EditorState.Articles)
            {
                Console.WriteLine($"- {a.Title} (Pages: {string.Join(",", a.Pages ?? new List<int>())})");
                if (a.Segments != null && a.Segments.Count > 0)
                {
                    foreach (var s in a.Segments)
                        Console.WriteLine($"   * seg {s.Display} (End={(s.End.HasValue? s.End.Value.ToString(): "(open)")}) WasNew={s.WasNew} OrigEnd={s.OriginalEnd}");
                }
            }
        }
    }
}

