using System;
using System.Collections.Generic;
namespace IndexEditor.Shared
{


    public static class EditorState
    {
        public static int CurrentPage { get; set; } = 8;
        public static List<Common.Shared.ArticleLine> Articles { get; set; } = new();
        public static Common.Shared.ArticleLine? ActiveArticle { get; set; }
        public static Common.Shared.Segment? ActiveSegment { get; set; }
        // Metadata extracted from _index.txt or folder name
        public static string? CurrentMagazine { get; set; }
        public static string? CurrentVolume { get; set; }
        public static string? CurrentNumber { get; set; }
        // Folder opened by the app (used to locate page image files)
        public static string? CurrentFolder { get; set; }
        public static event Action? StateChanged;
        public static void NotifyStateChanged() => StateChanged?.Invoke();
    }
}
