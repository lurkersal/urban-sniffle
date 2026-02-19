using IndexEditor.Shared;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace Common.Tests
{
    /// <summary>
    /// Helper to initialize DI services for common tests that use EditorState.
    /// </summary>
    public static class TestDIHelper
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();
        
        /// <summary>
        /// Initializes DI services for testing.
        /// Safe to call multiple times - only initializes once.
        /// </summary>
        public static void EnsureInitialized()
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    // Create real instances for testing
                    var editorState = new EditorStateService();
                    var editorActions = new EditorActionsService(editorState);
                    
                    // Set static instances
                    EditorState.SetInstance(editorState);
                    EditorActions.SetInstance(editorActions);
                    
                    _initialized = true;
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"TestDIHelper initialization failed: {ex}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Resets the EditorState to a clean state for a new test.
        /// Call this in test constructors or at the start of tests.
        /// </summary>
        public static void ResetState()
        {
            // Ensure initialized first
            EnsureInitialized();
            
            EditorState.Articles = new System.Collections.Generic.List<Common.Shared.ArticleLine>();
            EditorState.ActiveArticle = null;
            EditorState.ActiveSegment = null;
            EditorState.CurrentPage = 1;
        }
    }
}

