using Dapper;
using Npgsql;
using TheArchive.Models;

namespace TheArchive.Services;

/// <summary>
/// Database service for querying the existing magazine database
/// Uses Dapper for lightweight ORM
/// </summary>
public class ArchiveDatabase
{
    private readonly string _connectionString;

    public ArchiveDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

    #region Magazines

    /// <summary>
    /// Get all magazines with issue counts
    /// </summary>
    public async Task<List<Magazine>> GetMagazinesAsync()
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT 
                m.MagazineId,
                m.Name,
                m.LogoUrl,
                COUNT(i.IssueId) as IssueCount
            FROM Magazine m
            LEFT JOIN Issue i ON m.MagazineId = i.MagazineId
            GROUP BY m.MagazineId, m.Name, m.LogoUrl
            ORDER BY m.Name";
        
        var magazines = await conn.QueryAsync<Magazine>(sql);
        return magazines.ToList();
    }

    /// <summary>
    /// Get a single magazine by ID or slug
    /// </summary>
    public async Task<Magazine?> GetMagazineAsync(string idOrSlug)
    {
        using var conn = GetConnection();
        
        // Try to parse as integer ID
        if (int.TryParse(idOrSlug, out var id))
        {
            const string sql = @"
                SELECT 
                    m.MagazineId,
                    m.Name,
                    m.LogoUrl,
                    COUNT(i.IssueId) as IssueCount
                FROM Magazine m
                LEFT JOIN Issue i ON m.MagazineId = i.MagazineId
                WHERE m.MagazineId = @Id
                GROUP BY m.MagazineId, m.Name, m.LogoUrl";
            
            return await conn.QueryFirstOrDefaultAsync<Magazine>(sql, new { Id = id });
        }
        else
        {
            // Search by name (slug-like)
            const string sql = @"
                SELECT 
                    m.MagazineId,
                    m.Name,
                    m.LogoUrl,
                    COUNT(i.IssueId) as IssueCount
                FROM Magazine m
                LEFT JOIN Issue i ON m.MagazineId = i.MagazineId
                WHERE LOWER(REPLACE(m.Name, ' ', '-')) = LOWER(@Slug)
                GROUP BY m.MagazineId, m.Name, m.LogoUrl";
            
            return await conn.QueryFirstOrDefaultAsync<Magazine>(sql, new { Slug = idOrSlug });
        }
    }

    #endregion

    #region Issues

    /// <summary>
    /// Get all issues across all magazines, sorted by date descending
    /// </summary>
    public async Task<List<Issue>> GetAllIssuesAsync(int page = 1, int perPage = 50)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT 
                i.IssueId,
                i.MagazineId,
                i.Volume,
                i.Number,
                i.Year,
                i.LinkScanPerformed,
                m.Name as MagazineName,
                COUNT(DISTINCT c.ArticleId) as ArticleCount
            FROM Issue i
            JOIN Magazine m ON i.MagazineId = m.MagazineId
            LEFT JOIN Content c ON i.IssueId = c.IssueId
            GROUP BY i.IssueId, i.MagazineId, i.Volume, i.Number, i.Year, i.LinkScanPerformed, m.Name
            ORDER BY i.Year DESC, i.Volume DESC, i.Number DESC
            LIMIT @PerPage OFFSET @Offset";
        
        var offset = (page - 1) * perPage;
        var issues = await conn.QueryAsync<Issue>(sql, new { PerPage = perPage, Offset = offset });
        return issues.ToList();
    }

    /// <summary>
    /// Get filtered issues with advanced filter options
    /// </summary>
    public async Task<List<Issue>> GetFilteredIssuesAsync(
        int? yearFrom = null,
        int? yearTo = null,
        List<int>? magazineIds = null,
        int page = 1,
        int perPage = 500)
    {
        using var conn = GetConnection();
        
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();
        
        if (yearFrom.HasValue)
        {
            whereClauses.Add("i.Year >= @YearFrom");
            parameters.Add("YearFrom", yearFrom.Value);
        }
        
        if (yearTo.HasValue)
        {
            whereClauses.Add("i.Year <= @YearTo");
            parameters.Add("YearTo", yearTo.Value);
        }
        
        if (magazineIds != null && magazineIds.Any())
        {
            whereClauses.Add("i.MagazineId = ANY(@MagazineIds)");
            parameters.Add("MagazineIds", magazineIds.ToArray());
        }
        
        var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";
        
        var sql = $@"
            SELECT 
                i.IssueId,
                i.MagazineId,
                i.Volume,
                i.Number,
                i.Year,
                i.LinkScanPerformed,
                m.Name as MagazineName,
                COUNT(DISTINCT c.ArticleId) as ArticleCount,
                cover.ImagePath as CoverImagePath
            FROM Issue i
            JOIN Magazine m ON i.MagazineId = m.MagazineId
            LEFT JOIN Content c ON i.IssueId = c.IssueId
            LEFT JOIN (
                SELECT DISTINCT ON (mc.IssueId) mc.IssueId, mc.ImagePath
                FROM Content mc
                JOIN Article a ON mc.ArticleId = a.ArticleId
                JOIN Category cat ON a.CategoryId = cat.CategoryId
                WHERE cat.Name = 'Cover'
                ORDER BY mc.IssueId, mc.Page
            ) cover ON i.IssueId = cover.IssueId
            {whereClause}
            GROUP BY i.IssueId, i.MagazineId, i.Volume, i.Number, i.Year, i.LinkScanPerformed, m.Name, cover.ImagePath
            ORDER BY m.Name ASC, i.Volume ASC, i.Number ASC
            LIMIT @PerPage OFFSET @Offset";
        
        var offset = (page - 1) * perPage;
        parameters.Add("PerPage", perPage);
        parameters.Add("Offset", offset);
        
        var issues = await conn.QueryAsync<Issue>(sql, parameters);
        return issues.ToList();
    }

    /// <summary>
    /// Get issues for a specific magazine
    /// </summary>
    public async Task<List<Issue>> GetIssuesByMagazineAsync(int magazineId)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT 
                i.IssueId,
                i.MagazineId,
                i.Volume,
                i.Number,
                i.Year,
                i.LinkScanPerformed,
                m.Name as MagazineName,
                COUNT(DISTINCT c.ArticleId) as ArticleCount,
                MAX(c.Page) as PageCount,
                cover.ImagePath as CoverImagePath
            FROM Issue i
            JOIN Magazine m ON i.MagazineId = m.MagazineId
            LEFT JOIN Content c ON i.IssueId = c.IssueId
            LEFT JOIN (
                SELECT DISTINCT ON (mc.IssueId) mc.IssueId, mc.ImagePath
                FROM Content mc
                JOIN Article a ON mc.ArticleId = a.ArticleId
                JOIN Category cat ON a.CategoryId = cat.CategoryId
                WHERE cat.Name = 'Cover'
                ORDER BY mc.IssueId, mc.Page
            ) cover ON i.IssueId = cover.IssueId
            WHERE i.MagazineId = @MagazineId
            GROUP BY i.IssueId, i.MagazineId, i.Volume, i.Number, i.Year, i.LinkScanPerformed, m.Name, cover.ImagePath
            ORDER BY i.Volume ASC, i.Number ASC";
        
        var issues = await conn.QueryAsync<Issue>(sql, new { MagazineId = magazineId });
        return issues.ToList();
    }

    /// <summary>
    /// Get a single issue by ID
    /// </summary>
    public async Task<Issue?> GetIssueAsync(int issueId)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT 
                i.IssueId,
                i.MagazineId,
                i.Volume,
                i.Number,
                i.Year,
                i.LinkScanPerformed,
                m.Name as MagazineName,
                COUNT(DISTINCT c.ArticleId) as ArticleCount,
                MAX(c.Page) as PageCount,
                cover.ImagePath as CoverImagePath
            FROM Issue i
            JOIN Magazine m ON i.MagazineId = m.MagazineId
            LEFT JOIN Content c ON i.IssueId = c.IssueId
            LEFT JOIN (
                SELECT DISTINCT ON (mc.IssueId) mc.IssueId, mc.ImagePath
                FROM Content mc
                JOIN Article a ON mc.ArticleId = a.ArticleId
                JOIN Category cat ON a.CategoryId = cat.CategoryId
                WHERE cat.Name = 'Cover'
                ORDER BY mc.IssueId, mc.Page
            ) cover ON i.IssueId = cover.IssueId
            WHERE i.IssueId = @IssueId
            GROUP BY i.IssueId, i.MagazineId, i.Volume, i.Number, i.Year, i.LinkScanPerformed, m.Name, cover.ImagePath";
        
        return await conn.QueryFirstOrDefaultAsync<Issue>(sql, new { IssueId = issueId });
    }

    #endregion

    #region Articles

    /// <summary>
    /// Get articles for a specific issue
    /// </summary>
    public async Task<List<Article>> GetArticlesByIssueAsync(int issueId, string? categoryFilter = null)
    {
        using var conn = GetConnection();
        
        var sql = @"
            SELECT DISTINCT
                a.ArticleId,
                a.CategoryId,
                a.Title,
                cat.Name as CategoryName,
                c.IssueId,
                MIN(c.Page) as PageStart,
                STRING_AGG(DISTINCT contrib.Name, ', ') as Photographer,
                cm.ModelId,
                m.Name as ModelName,
                first_img.ImagePath as FirstImagePath
            FROM Article a
            JOIN Category cat ON a.CategoryId = cat.CategoryId
            JOIN Content c ON a.ArticleId = c.ArticleId
            LEFT JOIN ContentContributor cc ON c.ContentId = cc.ContentId
            LEFT JOIN Contributor contrib ON cc.ContributorId = contrib.ContributorId
            LEFT JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
            LEFT JOIN Model m ON cm.ModelId = m.ModelId
            LEFT JOIN (
                SELECT DISTINCT ON (c2.ArticleId) c2.ArticleId, c2.ImagePath
                FROM Content c2
                WHERE c2.ImagePath IS NOT NULL
                ORDER BY c2.ArticleId, c2.Page
            ) first_img ON a.ArticleId = first_img.ArticleId
            WHERE c.IssueId = @IssueId";
        
        if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter.ToLower() != "all")
        {
            sql += " AND LOWER(cat.Name) = LOWER(@Category)";
        }
        
        sql += @"
            GROUP BY a.ArticleId, a.CategoryId, a.Title, cat.Name, c.IssueId, cm.ModelId, m.Name, first_img.ImagePath
            ORDER BY MIN(c.Page) ASC";
        
        var articles = await conn.QueryAsync<Article>(sql, new { IssueId = issueId, Category = categoryFilter });
        return articles.ToList();
    }

    /// <summary>
    /// Get all articles (paginated) with optional category filter
    /// </summary>
    public async Task<List<Article>> GetAllArticlesAsync(string? category = null, int page = 1, int perPage = 50)
    {
        using var conn = GetConnection();
        
        var sql = @"
            SELECT DISTINCT
                a.ArticleId,
                a.CategoryId,
                a.Title,
                cat.Name as CategoryName,
                c.IssueId,
                MIN(c.Page) as PageStart
            FROM Article a
            JOIN Category cat ON a.CategoryId = cat.CategoryId
            JOIN Content c ON a.ArticleId = c.ArticleId";
        
        if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all")
        {
            sql += " WHERE LOWER(cat.Name) = LOWER(@Category)";
        }
        
        sql += @"
            GROUP BY a.ArticleId, a.CategoryId, a.Title, cat.Name, c.IssueId
            ORDER BY a.ArticleId DESC
            LIMIT @PerPage OFFSET @Offset";
        
        var offset = (page - 1) * perPage;
        var articles = await conn.QueryAsync<Article>(sql, new { Category = category, PerPage = perPage, Offset = offset });
        return articles.ToList();
    }

    /// <summary>
    /// Get a single article by ID
    /// </summary>
    public async Task<Article?> GetArticleAsync(int articleId)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT DISTINCT
                a.ArticleId,
                a.CategoryId,
                a.Title,
                cat.Name as CategoryName,
                c.IssueId,
                MIN(c.Page) as PageStart,
                STRING_AGG(DISTINCT contrib.Name, ', ') as Photographer,
                cm.ModelId,
                m.Name as ModelName
            FROM Article a
            JOIN Category cat ON a.CategoryId = cat.CategoryId
            LEFT JOIN Content c ON a.ArticleId = c.ArticleId
            LEFT JOIN ContentContributor cc ON c.ContentId = cc.ContentId
            LEFT JOIN Contributor contrib ON cc.ContributorId = contrib.ContributorId
            LEFT JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
            LEFT JOIN Model m ON cm.ModelId = m.ModelId
            WHERE a.ArticleId = @ArticleId
            GROUP BY a.ArticleId, a.CategoryId, a.Title, cat.Name, c.IssueId, cm.ModelId, m.Name";
        
        return await conn.QueryFirstOrDefaultAsync<Article>(sql, new { ArticleId = articleId });
    }

    #endregion

    #region Models

    /// <summary>
    /// Get all models with appearance counts
    /// </summary>
    public async Task<List<Model>> GetModelsAsync()
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT 
                m.ModelId,
                m.Name,
                m.YearOfBirth,
                m.BustSize,
                m.WaistSize,
                m.HipSize,
                m.CupSize,
                COUNT(DISTINCT cm.ArticleId) as AppearanceCount
            FROM Model m
            LEFT JOIN ContentModel cm ON m.ModelId = cm.ModelId
            GROUP BY m.ModelId, m.Name, m.YearOfBirth, m.BustSize, m.WaistSize, m.HipSize, m.CupSize
            ORDER BY m.Name";
        
        var models = await conn.QueryAsync<Model>(sql);
        return models.ToList();
    }

    /// <summary>
    /// Get a single model by ID or slug
    /// </summary>
    public async Task<Model?> GetModelAsync(string idOrSlug)
    {
        using var conn = GetConnection();
        
        // Try to parse as integer ID
        if (int.TryParse(idOrSlug, out var id))
        {
            const string sql = @"
                SELECT 
                    m.ModelId,
                    m.Name,
                    m.YearOfBirth,
                    m.BustSize,
                    m.WaistSize,
                    m.HipSize,
                    m.CupSize,
                    COUNT(DISTINCT cm.ArticleId) as AppearanceCount
                FROM Model m
                LEFT JOIN ContentModel cm ON m.ModelId = cm.ModelId
                WHERE m.ModelId = @Id
                GROUP BY m.ModelId, m.Name, m.YearOfBirth, m.BustSize, m.WaistSize, m.HipSize, m.CupSize";
            
            return await conn.QueryFirstOrDefaultAsync<Model>(sql, new { Id = id });
        }
        else
        {
            // Search by name (slug-like)
            const string sql = @"
                SELECT 
                    m.ModelId,
                    m.Name,
                    m.YearOfBirth,
                    m.BustSize,
                    m.WaistSize,
                    m.HipSize,
                    m.CupSize,
                    COUNT(DISTINCT cm.ArticleId) as AppearanceCount
                FROM Model m
                LEFT JOIN ContentModel cm ON m.ModelId = cm.ModelId
                WHERE LOWER(REPLACE(REPLACE(m.Name, ' ', '-'), '.', '')) = LOWER(@Slug)
                GROUP BY m.ModelId, m.Name, m.YearOfBirth, m.BustSize, m.WaistSize, m.HipSize, m.CupSize";
            
            return await conn.QueryFirstOrDefaultAsync<Model>(sql, new { Slug = idOrSlug });
        }
    }

    /// <summary>
    /// Get all articles featuring a specific model
    /// </summary>
    public async Task<List<Article>> GetArticlesByModelAsync(int modelId)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT DISTINCT
                a.ArticleId,
                a.CategoryId,
                a.Title,
                cat.Name as CategoryName,
                c.IssueId,
                MIN(c.Page) as PageStart,
                cm.ModelId,
                m.Name as ModelName
            FROM Article a
            JOIN Category cat ON a.CategoryId = cat.CategoryId
            JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
            JOIN Model m ON cm.ModelId = m.ModelId
            LEFT JOIN Content c ON a.ArticleId = c.ArticleId
            WHERE cm.ModelId = @ModelId
            GROUP BY a.ArticleId, a.CategoryId, a.Title, cat.Name, c.IssueId, cm.ModelId, m.Name
            ORDER BY a.ArticleId DESC";
        
        var articles = await conn.QueryAsync<Article>(sql, new { ModelId = modelId });
        return articles.ToList();
    }

    /// <summary>
    /// Get all issues featuring a specific model
    /// </summary>
    public async Task<List<Issue>> GetIssuesByModelAsync(int modelId)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT DISTINCT
                i.IssueId,
                i.MagazineId,
                i.Volume,
                i.Number,
                i.Year,
                i.LinkScanPerformed,
                mag.Name as MagazineName
            FROM Issue i
            JOIN Magazine mag ON i.MagazineId = mag.MagazineId
            JOIN Content c ON i.IssueId = c.IssueId
            JOIN Article a ON c.ArticleId = a.ArticleId
            JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
            WHERE cm.ModelId = @ModelId
            ORDER BY i.Year DESC, i.Volume DESC, i.Number DESC";
        
        var issues = await conn.QueryAsync<Issue>(sql, new { ModelId = modelId });
        return issues.ToList();
    }

    #endregion

    #region Contributors

    /// <summary>
    /// Get all contributors (photographers, etc.) with appearance counts
    /// </summary>
    public async Task<List<Contributor>> GetContributorsAsync()
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT 
                c.ContributorId,
                c.Name,
                COUNT(DISTINCT cc.ContentId) as AppearanceCount
            FROM Contributor c
            LEFT JOIN ContentContributor cc ON c.ContributorId = cc.ContributorId
            GROUP BY c.ContributorId, c.Name
            ORDER BY c.Name";
        
        var contributors = await conn.QueryAsync<Contributor>(sql);
        return contributors.ToList();
    }

    /// <summary>
    /// Get a single contributor by ID or slug
    /// </summary>
    public async Task<Contributor?> GetContributorAsync(string idOrSlug)
    {
        using var conn = GetConnection();
        
        // Try to parse as integer ID
        if (int.TryParse(idOrSlug, out var id))
        {
            const string sql = @"
                SELECT 
                    c.ContributorId,
                    c.Name,
                    COUNT(DISTINCT cc.ContentId) as AppearanceCount
                FROM Contributor c
                LEFT JOIN ContentContributor cc ON c.ContributorId = cc.ContributorId
                WHERE c.ContributorId = @Id
                GROUP BY c.ContributorId, c.Name";
            
            return await conn.QueryFirstOrDefaultAsync<Contributor>(sql, new { Id = id });
        }
        else
        {
            // Search by name (slug-like)
            const string sql = @"
                SELECT 
                    c.ContributorId,
                    c.Name,
                    COUNT(DISTINCT cc.ContentId) as AppearanceCount
                FROM Contributor c
                LEFT JOIN ContentContributor cc ON c.ContributorId = cc.ContributorId
                WHERE LOWER(REPLACE(REPLACE(c.Name, ' ', '-'), '.', '')) = LOWER(@Slug)
                GROUP BY c.ContributorId, c.Name";
            
            return await conn.QueryFirstOrDefaultAsync<Contributor>(sql, new { Slug = idOrSlug });
        }
    }

    /// <summary>
    /// Get all articles by a specific contributor
    /// </summary>
    public async Task<List<Article>> GetArticlesByContributorAsync(int contributorId)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT DISTINCT
                a.ArticleId,
                a.CategoryId,
                a.Title,
                cat.Name as CategoryName,
                c.IssueId,
                MIN(cont.Page) as PageStart
            FROM Article a
            JOIN Category cat ON a.CategoryId = cat.CategoryId
            JOIN Content cont ON a.ArticleId = cont.ArticleId
            JOIN ContentContributor cc ON cont.ContentId = cc.ContentId
            WHERE cc.ContributorId = @ContributorId
            GROUP BY a.ArticleId, a.CategoryId, a.Title, cat.Name, c.IssueId
            ORDER BY a.ArticleId DESC";
        
        var articles = await conn.QueryAsync<Article>(sql, new { ContributorId = contributorId });
        return articles.ToList();
    }

    /// <summary>
    /// Get all issues featuring a specific contributor
    /// </summary>
    public async Task<List<Issue>> GetIssuesByContributorAsync(int contributorId)
    {
        using var conn = GetConnection();
        const string sql = @"
            SELECT DISTINCT
                i.IssueId,
                i.MagazineId,
                i.Volume,
                i.Number,
                i.Year,
                i.LinkScanPerformed,
                mag.Name as MagazineName
            FROM Issue i
            JOIN Magazine mag ON i.MagazineId = mag.MagazineId
            JOIN Content c ON i.IssueId = c.IssueId
            JOIN ContentContributor cc ON c.ContentId = cc.ContentId
            WHERE cc.ContributorId = @ContributorId
            ORDER BY mag.Name ASC, i.Volume ASC, i.Number ASC";
        
        var issues = await conn.QueryAsync<Issue>(sql, new { ContributorId = contributorId });
        return issues.ToList();
    }

    #endregion

    #region Search

    /// <summary>
    /// Search across magazines, issues, articles, and models
    /// </summary>
    public async Task<object> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new { magazines = new List<Magazine>(), issues = new List<Issue>(), models = new List<Model>() };
        }

        using var conn = GetConnection();
        
        var searchPattern = $"%{query}%";
        
        // Search magazines
        var magazines = await conn.QueryAsync<Magazine>(@"
            SELECT MagazineId, Name, LogoUrl
            FROM Magazine
            WHERE Name ILIKE @Pattern
            LIMIT 10", new { Pattern = searchPattern });
        
        // Search issues (by magazine name)
        var issues = await conn.QueryAsync<Issue>(@"
            SELECT i.IssueId, i.MagazineId, i.Volume, i.Number, i.Year, m.Name as MagazineName
            FROM Issue i
            JOIN Magazine m ON i.MagazineId = m.MagazineId
            WHERE m.Name ILIKE @Pattern
            LIMIT 10", new { Pattern = searchPattern });
        
        // Search models
        var models = await conn.QueryAsync<Model>(@"
            SELECT ModelId, Name
            FROM Model
            WHERE Name ILIKE @Pattern
            LIMIT 10", new { Pattern = searchPattern });
        
        return new
        {
            magazines = magazines.ToList(),
            issues = issues.ToList(),
            models = models.ToList()
        };
    }

    #endregion
}

