namespace FindLinks.Services
{
    using System.Collections.Generic;

    public interface IDatabaseRepository
    {
        bool GetLinkScanPerformed(int issueId);
        void SetLinkScanPerformed(int issueId, bool performed);
        List<(int volume, int number)> GetAllIssuesForMagazine(int magazineId); // Issue
        List<int> GetNumbersForMagazineVolume(int magazineId, int volume); // Issue
        int GetMagazineId(string magazineName); // Magazine
        int? GetExistingIssueId(int magazineId, int volume, int number); // Issue
        List<int> GetPagesForIssue(int issueId); // Content
        Dictionary<int, string?> GetPageImagePathsForIssue(int issueId); // Content
        Dictionary<int, string> GetPageCategoriesForIssue(int issueId); // Category
        void InsertIssueLinks(int contentId, IEnumerable<(int magazineId, int volume, int number, int page)> linkedIssues); // IssueLink
        bool IssueLinksExistForIssue(int issueId); // IssueLink
    }
}
