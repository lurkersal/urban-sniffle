namespace IndexEditor.Services
{
    /// <summary>
    /// Service interface for managing active segments
    /// </summary>
    public interface ISegmentManagementService
    {
        /// <summary>
        /// Ends the currently active segment by setting its end page and updating the article's pages.
        /// </summary>
        /// <param name="dataContext">The DataContext (typically EditorStateViewModel) for syncing view model</param>
        void EndActiveSegment(object? dataContext);

        /// <summary>
        /// Adds a new active segment at the current page.
        /// </summary>
        /// <returns>True if a new active segment was created</returns>
        bool AddSegmentAtCurrentPage();
    }
}

