using System;
using System.Collections.Generic;
using Common.Shared;
using IndexEditor.Shared;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Tools
{
    public static class DemoRunner
    {
        public static void Run()
        {
            DebugLogger.Log("Interactive Demo: segment lifecycle (add -> cancel -> reopen -> end)");

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
            DebugLogger.Log("-- Add Segment at current page (5) --");
            var seg = new Segment(5);
            seg.WasNew = true;
            a1.Segments.Add(seg);
            EditorState.ActiveSegment = seg;
            EditorState.NotifyStateChanged();
            PrintState("After AddSegment");

            // Cancel segment -> should remove because WasNew
            DebugLogger.Log("-- Cancel Segment (should remove new segment) --");
            if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
            {
                var s = EditorState.ActiveSegment;
                var art = EditorState.ActiveArticle;
                if (s.WasNew)
                {
                    art.Segments.Remove(s);
                    DebugLogger.Log("Removed newly created segment from article");
                }
                else if (s.OriginalEnd.HasValue)
                {
                    s.End = s.OriginalEnd;
                    s.OriginalEnd = null;
                    DebugLogger.Log("Restored original end on reopened segment");
                }
                else
                {
                    s.End = s.Start;
                    DebugLogger.Log("Closed segment to single page (fallback)");
                }
                s.WasNew = false;
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            PrintState("After Cancel");

            // Create a closed segment and then reopen it (simulate clicking it)
            DebugLogger.Log("-- Create closed segment 7-8 then reopen it --");
            var closed = new Segment(7, 8);
            a1.Segments.Add(closed);
            PrintState("After adding closed segment 7-8");

            DebugLogger.Log("Reopening closed segment (should set OriginalEnd and clear End)");
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
            DebugLogger.Log("-- Cancel reopened segment (should restore 7-8) --");
            if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
            {
                var s = EditorState.ActiveSegment;
                if (s.WasNew)
                {
                    var art = EditorState.ActiveArticle;
                    art.Segments.Remove(s);
                    DebugLogger.Log("Removed new segment");
                }
                else if (s.OriginalEnd.HasValue)
                {
                    s.End = s.OriginalEnd;
                    s.OriginalEnd = null;
                    DebugLogger.Log($"Restored original end {s.End}");
                }
                else
                {
                    s.End = s.Start;
                    DebugLogger.Log("Closed segment fallback");
                }
                s.WasNew = false;
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            PrintState("After Cancel Reopen");

            // Now reopen again and End the segment
            DebugLogger.Log("-- Reopen and End segment (should add pages to article) --");
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

            DebugLogger.Log("Demo complete.");
        }

        static void PrintState(string label)
        {
            DebugLogger.Log($"\n== {label} ==");
            DebugLogger.Log($"CurrentPage: {EditorState.CurrentPage}");
            DebugLogger.Log($"ActiveArticle: {EditorState.ActiveArticle?.Title ?? "(none)"}");
            DebugLogger.Log($"ActiveSegment: {(EditorState.ActiveSegment == null ? "(none)" : EditorState.ActiveSegment.Display)}");
            DebugLogger.Log("Articles and segments:");
            foreach (var a in EditorState.Articles)
            {
                DebugLogger.Log($"- {a.Title} (Pages: {string.Join(",", a.Pages ?? new List<int>())})");
                if (a.Segments != null && a.Segments.Count > 0)
                {
                    foreach (var s in a.Segments)
                        DebugLogger.Log($"   * seg {s.Display} (End={(s.End.HasValue? s.End.Value.ToString(): "(open)")}) WasNew={s.WasNew} OrigEnd={s.OriginalEnd}");
                }
            }
        }
    }
}
