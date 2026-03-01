using System;
using System.Linq;
using System.Collections.Generic;
using Common.Shared;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Shared
{
    public static class IndexSaver
    {
        // Save the current EditorState.Articles and metadata into _index.json under folder.
        // Phase 1: Save JSON format only
        public static void SaveIndex(string folder, List<MagazineLink>? links = null)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("folder is required", nameof(folder));
            
            // Use the new JSON serializer with optional links
            IndexJsonSerializer.SaveToJson(
                folder,
                EditorState.CurrentMagazine ?? string.Empty,
                EditorState.CurrentVolume ?? string.Empty,
                EditorState.CurrentNumber ?? string.Empty,
                EditorState.CurrentYear ?? string.Empty,
                EditorState.Articles.ToList(),
                links
            );
            
            // Clear the unsaved changes flag after successful save
            EditorState.HasUnsavedChanges = false;
        }
    }
}
