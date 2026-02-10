namespace IndexEditor.Views
{
    public class PageControllerBridge : IPageControllerBridge
    {
        private readonly PageControllerView _view;
        public PageControllerBridge(PageControllerView view) { _view = view; }
        public bool AddSegmentAtCurrentPage() => _view.AddSegmentAtCurrentPage();
        public void CreateNewArticle() => _view.CreateNewArticle();
        public void EndActiveSegment() => _view.EndActiveSegment();
        public void MoveLeft() => _view.MoveLeft();
        public void MoveRight() => _view.MoveRight();
    }
}
