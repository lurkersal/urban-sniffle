using IndexEditor.Shared;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Tests
{
    /// <summary>
    /// Helper to initialize DI services for tests.
    /// Automatically initializes on first access.
    /// </summary>
    public static class TestDIHelper
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();
        private static IndexEditor.Services.IndexFileService? _indexFileService;
        
        /// <summary>
        /// Gets the IIndexFileService instance for testing.
        /// </summary>
        public static IndexEditor.Services.IIndexFileService IndexFileService
        {
            get
            {
                EnsureInitialized();
                return _indexFileService!;
            }
        }
        
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
                    
                    // Use NullLogger for tests
                    var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<IndexEditor.Services.IndexFileService>.Instance;
                    _indexFileService = new IndexEditor.Services.IndexFileService(logger);
                    
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
        /// Call this in test constructors - will auto-initialize if needed.
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



