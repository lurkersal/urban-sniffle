using System;
using Common.Shared;

namespace IndexEditor.Shared
{
    public static class EditorActions
    {
        // Cancel the currently active segment if one exists.
        // Behavior:
        // - If segment.WasNew -> remove it from its article
        // - Else if segment.OriginalEnd.HasValue -> restore OriginalEnd
        // - Else -> set End = Start (close to single page)
        // After cancellation, set WasNew=false, clear ActiveSegment and notify state changed.
        public static void CancelActiveSegment()
        {
            try
            {
                var seg = EditorState.ActiveSegment;
                if (seg == null || !seg.IsActive) return;
                var art = EditorState.ActiveArticle;
                if (seg.WasNew)
                {
                    try { art?.Segments?.Remove(seg); } catch { }
                }
                else if (seg.OriginalEnd.HasValue)
                {
                    seg.End = seg.OriginalEnd;
                    seg.OriginalEnd = null;
                }
                else
                {
                    seg.End = seg.Start;
                }
                seg.WasNew = false;
                try { seg.CurrentPreviewEnd = null; } catch { }
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            catch { }
        }
    }
}
