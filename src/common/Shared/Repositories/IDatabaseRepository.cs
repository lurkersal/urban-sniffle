using common.Shared.Models;

namespace common.Shared.Repositories;

public interface IDatabaseRepository
{
    int GetMagazineId(string magazineName); // Magazine
    int? GetExistingIssueId(int magazineId, int volume, int number); // Issue
    int InsertIssue(int magazineId, int volume, int number, int year); // Issue
    int GetOrCreateCategoryId(string categoryName); // Category
    void CreateCategory(string categoryName); // Category
    List<string> GetAllCategories(); // Category
    int? GetOrCreateModelId(string modelName, int? bustSize, int? waistSize, int? hipSize, string? cupSize); // Model
    int InsertArticle(int categoryId, string? title); // Article
    int InsertContent(int issueId, int page, int articleId, string? imagePath); // Content
    void LinkArticleToModel(int articleId, int modelId, int? age, string? measurements); // ContentModel
    int GetOrCreateContributorId(string contributorName); // Contributor
    void LinkContentToContributor(int contentId, int contributorId); // ContentContributor
    ExistingContentMatch? GetExistingContent(int issueId, ContentLine contentLine); // Content
    void DeleteContent(List<int> contentIds); // Content
}
