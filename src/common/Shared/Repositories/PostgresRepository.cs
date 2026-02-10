using common.Shared.Interfaces;
using common.Shared.Models;
using Npgsql;

namespace common.Shared.Repositories;

public class PostgresRepository : IDatabaseRepository
{
    private readonly NpgsqlConnection _connection;

    public PostgresRepository(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public int GetMagazineId(string magazineName)
    {
        using var cmd = new NpgsqlCommand("SELECT MagazineId FROM Magazine WHERE Name = @name", _connection);
        cmd.Parameters.AddWithValue("name", magazineName);
        var result = cmd.ExecuteScalar();
        return result != null ? (int)result : 0;
    }

    public int? GetExistingIssueId(int magazineId, int volume, int number)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT IssueId FROM Issue WHERE MagazineId = @mid AND Volume = @vol AND Number = @num", 
            _connection);
        cmd.Parameters.AddWithValue("mid", magazineId);
        cmd.Parameters.AddWithValue("vol", volume);
        cmd.Parameters.AddWithValue("num", number);
        var result = cmd.ExecuteScalar();
        return result != null ? (int)result : null;
    }

    public int InsertIssue(int magazineId, int volume, int number, int year)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO Issue (MagazineId, Volume, Number, Year) VALUES (@mid, @vol, @num, @year) RETURNING IssueId",
            _connection);
        cmd.Parameters.AddWithValue("mid", magazineId);
        cmd.Parameters.AddWithValue("vol", volume);
        cmd.Parameters.AddWithValue("num", number);
        cmd.Parameters.AddWithValue("year", year);
        return (int)cmd.ExecuteScalar()!;
    }

    public int GetOrCreateCategoryId(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return 0;

        using var cmd = new NpgsqlCommand(
            "SELECT CategoryId FROM Category WHERE lower(Name) = lower(@name)", 
            _connection);
        cmd.Parameters.AddWithValue("name", categoryName);
        var result = cmd.ExecuteScalar();
        return result != null ? (int)result : 0;
    }

    public void CreateCategory(string categoryName)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO Category (Name) VALUES (@name)", 
            _connection);
        cmd.Parameters.AddWithValue("name", categoryName);
        cmd.ExecuteNonQuery();
    }

    public List<string> GetAllCategories()
    {
        var categories = new List<string>();
        using var cmd = new NpgsqlCommand(
            "SELECT Name FROM Category ORDER BY Name",
            _connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            categories.Add(reader.GetString(0));
        }
        return categories;
    }

    public int? GetOrCreateModelId(string modelName, int? bustSize, int? waistSize, int? hipSize, string? cupSize)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return null;

        // Check if model exists and get measurements
        using var checkCmd = new NpgsqlCommand(
            "SELECT ModelId, BustSize, WaistSize, HipSize, CupSize FROM Model WHERE Name = @name",
            _connection);
        checkCmd.Parameters.AddWithValue("name", modelName);
        using var reader = checkCmd.ExecuteReader();
        if (reader.Read())
        {
            int modelId = reader.GetInt32(0);
            int? dbBust = reader.IsDBNull(1) ? null : reader.GetInt32(1);
            int? dbWaist = reader.IsDBNull(2) ? null : reader.GetInt32(2);
            int? dbHip = reader.IsDBNull(3) ? null : reader.GetInt32(3);
            string? dbCup = reader.IsDBNull(4) ? null : reader.GetString(4);

            bool needsUpdate = false;
            if ((bustSize.HasValue && bustSize != dbBust) ||
                (waistSize.HasValue && waistSize != dbWaist) ||
                (hipSize.HasValue && hipSize != dbHip) ||
                (!string.IsNullOrEmpty(cupSize) && cupSize != dbCup))
            {
                needsUpdate = true;
            }
            reader.Close();
            if (needsUpdate)
            {
                using var updateCmd = new NpgsqlCommand(
                    "UPDATE Model SET BustSize = @bust, WaistSize = @waist, HipSize = @hip, CupSize = @cup WHERE ModelId = @id",
                    _connection);
                updateCmd.Parameters.AddWithValue("bust", bustSize ?? (object)DBNull.Value);
                updateCmd.Parameters.AddWithValue("waist", waistSize ?? (object)DBNull.Value);
                updateCmd.Parameters.AddWithValue("hip", hipSize ?? (object)DBNull.Value);
                updateCmd.Parameters.AddWithValue("cup", string.IsNullOrEmpty(cupSize) ? DBNull.Value : cupSize);
                updateCmd.Parameters.AddWithValue("id", modelId);
                updateCmd.ExecuteNonQuery();
            }
            return modelId;
        }
        reader.Close();

        // Insert new model
        using var insCmd = new NpgsqlCommand(
            "INSERT INTO Model (Name, BustSize, WaistSize, HipSize, CupSize) " +
            "VALUES (@name, @bust, @waist, @hip, @cup) RETURNING ModelId",
            _connection);
        insCmd.Parameters.AddWithValue("name", modelName);
        insCmd.Parameters.AddWithValue("bust", bustSize ?? (object)DBNull.Value);
        insCmd.Parameters.AddWithValue("waist", waistSize ?? (object)DBNull.Value);
        insCmd.Parameters.AddWithValue("hip", hipSize ?? (object)DBNull.Value);
        insCmd.Parameters.AddWithValue("cup", string.IsNullOrEmpty(cupSize) ? DBNull.Value : cupSize);
        return (int)insCmd.ExecuteScalar()!;
    }

    public int InsertArticle(int categoryId, string? title)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO Article (CategoryId, Title) " +
            "VALUES (@catid, @title) RETURNING ArticleId",
            _connection);
        cmd.Parameters.AddWithValue("catid", categoryId);
        cmd.Parameters.AddWithValue("title", string.IsNullOrEmpty(title) ? DBNull.Value : title);
        return (int)cmd.ExecuteScalar()!;
    }
    
    public int GetOrCreateContributorId(string contributorName, string role)
    {
        if (string.IsNullOrWhiteSpace(contributorName))
            return 0;
            
        // Check if contributor exists
        using var checkCmd = new NpgsqlCommand(
            "SELECT ContributorId FROM Contributor WHERE Name = @name AND Role = @role",
            _connection);
        checkCmd.Parameters.AddWithValue("name", contributorName);
        checkCmd.Parameters.AddWithValue("role", role);
        var existingId = checkCmd.ExecuteScalar();
        
        if (existingId != null)
            return (int)existingId;
            
        // Insert new contributor
        using var insCmd = new NpgsqlCommand(
            "INSERT INTO Contributor (Name, Role) VALUES (@name, @role) RETURNING ContributorId",
            _connection);
        insCmd.Parameters.AddWithValue("name", contributorName);
        insCmd.Parameters.AddWithValue("role", role);
        return (int)insCmd.ExecuteScalar()!;
    }
    
    public void LinkContentToContributor(int contentId, int contributorId)
    {
        if (contributorId == 0)
            return;
            
        using var cmd = new NpgsqlCommand(
            "INSERT INTO ContentContributor (ContentId, ContributorId) VALUES (@cid, @contrib) " +
            "ON CONFLICT (ContentId, ContributorId) DO NOTHING",
            _connection);
        cmd.Parameters.AddWithValue("cid", contentId);
        cmd.Parameters.AddWithValue("contrib", contributorId);
        cmd.ExecuteNonQuery();
    }

    public int InsertContent(int issueId, int page, int articleId, string? imagePath)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO Content (IssueId, Page, ArticleId, ImagePath) " +
            "VALUES (@issueid, @page, @artid, @imgpath) RETURNING ContentId",
            _connection);
        cmd.Parameters.AddWithValue("issueid", issueId);
        cmd.Parameters.AddWithValue("page", page);
        cmd.Parameters.AddWithValue("artid", articleId);
        cmd.Parameters.AddWithValue("imgpath", string.IsNullOrEmpty(imagePath) ? DBNull.Value : imagePath);
        return (int)cmd.ExecuteScalar()!;
    }

    public void LinkArticleToModel(int articleId, int modelId, int? age, string? measurements)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO ContentModel (ArticleId, ModelId, Age, Measurements) VALUES (@aid, @mid, @age, @measurements)",
            _connection);
        cmd.Parameters.AddWithValue("aid", articleId);
        cmd.Parameters.AddWithValue("mid", modelId);
        cmd.Parameters.AddWithValue("age", age ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("measurements", string.IsNullOrEmpty(measurements) ? DBNull.Value : measurements);
        cmd.ExecuteNonQuery();
    }

    public ExistingContentMatch? GetExistingContent(int issueId, ContentLine contentLine)
    {
        try
        {
            using var cmd = new NpgsqlCommand(
                "SELECT mc.ContentId, mc.Page, c.Name as Category, a.Title, m.Name as ModelName, m.Age, a.Photographer, " +
                "CASE WHEN m.BustSize IS NOT NULL THEN " +
                "  CAST(m.BustSize AS VARCHAR) || COALESCE(m.CupSize, '') || '-' || CAST(m.WaistSize AS VARCHAR) || '-' || CAST(m.HipSize AS VARCHAR) " +
                "ELSE '' END as ModelSize " +
                "FROM Content mc " +
                "JOIN Article a ON mc.ArticleId = a.ArticleId " +
                "JOIN Category c ON a.CategoryId = c.CategoryId " +
                "LEFT JOIN ContentModel cm ON a.ArticleId = cm.ArticleId " +
                "LEFT JOIN Model m ON cm.ModelId = m.ModelId " +
                "WHERE mc.IssueId = @issueid AND mc.Page = ANY(@pages) ORDER BY mc.Page",
                _connection);
            cmd.Parameters.AddWithValue("issueid", issueId);
            cmd.Parameters.AddWithValue("pages", contentLine.Pages.ToArray());

            var dbRows = new List<(int contentId, int page, string category, string title, string modelName, int? age, string photographer, string modelSize)>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                dbRows.Add((
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                    reader.IsDBNull(3) ? "" : reader.GetString(3),
                    reader.IsDBNull(4) ? "" : reader.GetString(4),
                    reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                    reader.IsDBNull(6) ? "" : reader.GetString(6),
                    reader.IsDBNull(7) ? "" : reader.GetString(7)
                ));
            }

            if (dbRows.Count == 0)
                return null;

            // Check if all db rows match the line
            bool matches = dbRows.Count == contentLine.Pages.Count;
            if (matches)
            {
                foreach (var row in dbRows)
                {
                    if (!row.category.Equals(contentLine.Category, StringComparison.OrdinalIgnoreCase) ||
                        row.title != contentLine.Title ||
                        row.modelName != contentLine.ModelName ||
                        row.age != contentLine.Age ||
                        row.photographer != contentLine.Photographer ||
                        row.modelSize != contentLine.ModelSize)
                    {
                        matches = false;
                        break;
                    }
                }
            }

            var firstRow = dbRows[0];
            string dbRepresentation = $"{string.Join("|", dbRows.Select(r => r.page))}, {firstRow.category}, {firstRow.title}, {firstRow.modelName}, {firstRow.age}, {firstRow.photographer}, {firstRow.modelSize}";

            return new ExistingContentMatch
            {
                Matches = matches,
                DbRepresentation = dbRepresentation,
                ContentIds = dbRows.Select(r => r.contentId).ToList()
            };
        }
        catch
        {
            return null;
        }
    }

    public void DeleteContent(List<int> contentIds)
    {
        if (contentIds == null || contentIds.Count == 0)
            return;

        using var delJunctionCmd = new NpgsqlCommand(
            "DELETE FROM ContentModel WHERE ContentId = ANY(@ids)", 
            _connection);
        delJunctionCmd.Parameters.AddWithValue("ids", contentIds.ToArray());
        delJunctionCmd.ExecuteNonQuery();

        using var cmd = new NpgsqlCommand(
            "DELETE FROM Content WHERE ContentId = ANY(@ids)", 
            _connection);
        cmd.Parameters.AddWithValue("ids", contentIds.ToArray());
        cmd.ExecuteNonQuery();
    }
}
