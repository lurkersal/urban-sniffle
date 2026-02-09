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
        // Whether the Article Editor control (or its children) currently has keyboard focus.
        // When true, global arrow-key handlers should avoid changing the current page so editor key navigation works normally.
        public static bool IsArticleEditorFocused { get; set; } = false;
        // Metadata extracted from _index.txt or folder name
        public static string? CurrentMagazine { get; set; }
        public static string? CurrentVolume { get; set; }
        public static string? CurrentNumber { get; set; }
        // Folder opened by the app (used to locate page image files)
        public static string? CurrentFolder { get; set; }
        // Flag to disable loading/showing images (can be set via --no-images command-line argument)
        public static bool ShowImages { get; set; } = true;
        public static event Action? StateChanged;
        public static void NotifyStateChanged() => StateChanged?.Invoke();
    }
}
