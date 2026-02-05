using System;
using System.Collections.Generic;
namespace IndexEditor.Shared
{


    public class Segment
    {
        public int Start { get; set; }
        public int? End { get; set; }
        public bool IsActive => !End.HasValue;
    }

    public static class EditorState
    {
        public static int CurrentPage { get; set; } = 8;
        public static List<Common.Shared.ArticleLine> Articles { get; set; } = new();
        public static Common.Shared.ArticleLine? ActiveArticle { get; set; }
        public static Segment? ActiveSegment { get; set; }
        public static event Action? StateChanged;
        public static void NotifyStateChanged() => StateChanged?.Invoke();
    }
}
