namespace IndexEditor.Views
{
    // Minimal no-op implementation registered as a placeholder until MainWindow provides the real bridge.
    internal class NullPageControllerBridge : IPageControllerBridge
    {
        public bool AddSegmentAtCurrentPage() => false; // indicates segment not added
        public void CreateNewArticle() { /* no-op */ }
        public void EndActiveSegment() { /* no-op */ }
        public void MoveLeft() { /* no-op */ }
        public void MoveRight() { /* no-op */ }
    }
}

