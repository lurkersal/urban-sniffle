namespace IndexEditor.Services
{
    /// <summary>
    /// Service interface for coordinating page navigation
    /// </summary>
    public interface IPageNavigationCoordinator
    {
        /// <summary>
        /// Moves to the previous available page
        /// </summary>
        /// <returns>The new page number, or null if unable to move</returns>
        int? MoveToPreviousPage();

        /// <summary>
        /// Moves to the next available page
        /// </summary>
        /// <returns>The new page number, or null if unable to move</returns>
        int? MoveToNextPage();
    }
}

