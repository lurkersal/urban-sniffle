namespace IndexEditor.Views
{
    public interface IPageControllerBridge
    {
        bool AddSegmentAtCurrentPage();
        void CreateNewArticle();
        void EndActiveSegment();
        void MoveLeft();
        void MoveRight();
    }
}

