using Dapper;
using Npgsql;
using MagazineViewer.Models;
namespace MagazineViewer.Services
{
    public class MagazineDatabase
    {
        public async Task<IEnumerable<ModelWithArticleTypes>> SearchModelsWithArticleTypesAsync(SearchFilters filters)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT m.*, 
                    COALESCE(ac.ArticleCount, 0) AS ArticleCount,
                    COALESCE(covers.CoverCount, 0) AS CoverCount,
                    (COALESCE(ac.ArticleCount, 0) - COALESCE(covers.CoverCount, 0)) AS ModelArticleCount
                FROM model m
                LEFT JOIN (
                    SELECT cm.modelid, COUNT(DISTINCT cm.articleid) AS ArticleCount
                    FROM contentmodel cm
                    GROUP BY cm.modelid
                ) ac ON m.modelid = ac.modelid
                LEFT JOIN (
                    SELECT cm.modelid, COUNT(DISTINCT cm.articleid) AS CoverCount
                    FROM contentmodel cm
                    JOIN article a ON cm.articleid = a.articleid
                    JOIN category c ON a.categoryid = c.categoryid
                    WHERE c.name = 'Cover'
                    GROUP BY cm.modelid
                ) covers ON m.modelid = covers.modelid
                WHERE 1=1";
            var parameters = new Dapper.DynamicParameters();
            if (!string.IsNullOrEmpty(filters.ModelName))
            {
                sql += " AND m.Name ILIKE @ModelName";
                parameters.Add("ModelName", $"%{filters.ModelName}%");
            }
            if (!string.IsNullOrEmpty(filters.ArticleTitle))
            {
                sql += @" AND EXISTS (SELECT 1 FROM contentmodel cm JOIN article a ON cm.articleid = a.articleid WHERE cm.modelid = m.modelid AND a.title ILIKE @ArticleTitle)";
                parameters.Add("ArticleTitle", $"%{filters.ArticleTitle}%");
            }
            if (filters.BustSize.HasValue)
            {
                sql += " AND m.BustSize = @BustSize";
                parameters.Add("BustSize", filters.BustSize);
            }
            if (filters.WaistSize.HasValue)
            {
                sql += " AND m.WaistSize = @WaistSize";
                parameters.Add("WaistSize", filters.WaistSize);
            }
            if (filters.HipSize.HasValue)
            {
                sql += " AND m.HipSize = @HipSize";
                parameters.Add("HipSize", filters.HipSize);
            }
            sql += " ORDER BY m.Name";
            var models = (await conn.QueryAsync<ModelWithArticleTypes>(sql, parameters)).ToList();

            // Get first article image path for each model (any article, earliest by issue/page)
            var modelIds = models.Select(m => m.ModelId).ToList();
            if (modelIds.Count > 0)
            {
                var articleSql = @"
                    SELECT cm.modelid, mc.imagepath
                    FROM contentmodel cm
                    JOIN content mc ON mc.articleid = cm.articleid
                    WHERE cm.modelid = ANY(@ModelIds)
                    ORDER BY cm.modelid, mc.issueid, mc.page
                ";
                var articleResults = await conn.QueryAsync<(int ModelId, string? ImagePath)>(articleSql, new { ModelIds = modelIds.ToArray() });
                var firstArticleDict = articleResults
                    .GroupBy(x => x.ModelId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.ImagePath)
                            .FirstOrDefault(path => !string.IsNullOrEmpty(path))
                    );
                foreach (var m in models)
                {
                    if (firstArticleDict.TryGetValue(m.ModelId, out var path))
                    {
                        m.FirstArticleImagePath = path;
                    }
                }
            }
            return models;
        }
        public async Task<Issue?> FindIssueByVolNoAsync(string magazineName, int volume, int number)
        {
            using var conn = GetConnection();
            var sql = @"SELECT i.* FROM issue i JOIN magazine m ON i.magazineid = m.magazineid WHERE m.name = @MagazineName AND i.volume = @Volume AND i.number = @Number LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<Issue>(sql, new { MagazineName = magazineName, Volume = volume, Number = number });
        }
    private readonly string _connectionString;

    public MagazineDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<LinkedIssueView>> GetLinkedIssuesForArticleAsync(int articleId)
    {
        using var conn = GetConnection();
        // Get all contentids for this article
        var contentIds = (await conn.QueryAsync<int>(
            "SELECT contentid FROM content WHERE articleid = @ArticleId",
            new { ArticleId = articleId }
        )).ToArray();
        // DEBUG: log contentIds
        try
        {
            var logPath = System.IO.Path.Combine("..", "debug.log");
            using (var log = System.IO.File.AppendText(logPath))
            {
                log.WriteLine($"[DEBUG] GetLinkedIssuesForArticleAsync: ArticleId={articleId}, ContentIds=[{string.Join(",", contentIds)}]");
                log.Flush();
            }
        }
        catch { }
        if (contentIds.Length == 0)
            return Enumerable.Empty<LinkedIssueView>();
        // Fallback: build IN clause manually
        var inClause = string.Join(",", contentIds);
        var sql = $@"SELECT DISTINCT
    il.linkedmagazineid AS LinkedMagazineId,
    il.linkedvolume AS LinkedVolume,
    il.linkednumber AS LinkedNumber,
    il.page AS Page,
    i.issueid AS IssueId,
    m.name AS MagazineName,
    i.year AS Year,
    NULL AS CoverImagePath
    FROM issuelink il
    LEFT JOIN issue i ON
        i.magazineid = il.linkedmagazineid
        AND i.volume = il.linkedvolume
        AND i.number = il.linkednumber
    LEFT JOIN magazine m ON m.magazineid = il.linkedmagazineid
    WHERE il.contentid IN ({inClause})
    ORDER BY il.linkedvolume, il.linkednumber, il.page";
        // Log the final SQL
        try
        {
            var logPath = System.IO.Path.Combine("..", "debug.log");
            using (var log = System.IO.File.AppendText(logPath))
            {
                log.WriteLine($"[DEBUG] Final SQL: {sql}");
                log.Flush();
            }
        }
        catch { }
        return await conn.QueryAsync<LinkedIssueView>(sql);
    }

    private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

    public async Task<IEnumerable<int>> GetYearsAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<int>("SELECT DISTINCT year FROM issue ORDER BY year DESC");
    }

    public async Task<IEnumerable<MagazineViewer.Models.ArticleResult>> SearchArticlesAsync(int? magazineId, string? category, int? year, string? keyword)
    {
        using var conn = GetConnection();
        var sql = @"
            SELECT a.articleid, mc.issueid, a.title, mag.name as MagazineName, c.name as CategoryName, i.year, i.number as IssueNumber, MIN(mc.page) as Page
            FROM content mc
            JOIN article a ON mc.articleid = a.articleid
            JOIN category c ON a.categoryid = c.categoryid
            JOIN issue i ON mc.issueid = i.issueid
            JOIN magazine mag ON i.magazineid = mag.magazineid
            LEFT JOIN contentmodel cm ON a.articleid = cm.articleid
            LEFT JOIN model m ON cm.modelid = m.modelid
            WHERE 1=1";
        var parameters = new Dapper.DynamicParameters();
        if (magazineId.HasValue)
        {
            sql += " AND mag.MagazineId = @MagazineId";
            parameters.Add("MagazineId", magazineId);
        }
        if (!string.IsNullOrEmpty(category))
        {
            sql += " AND c.Name = @Category";
            parameters.Add("Category", category);
        }
        if (year.HasValue)
        {
            sql += " AND i.Year = @Year";
            parameters.Add("Year", year);
        }
        if (!string.IsNullOrEmpty(keyword))
        {
            sql += " AND (a.Title ILIKE @Keyword OR m.Name ILIKE @Keyword)";
            parameters.Add("Keyword", $"%{keyword}%");
        }
        sql += " GROUP BY a.ArticleId, mc.IssueId, a.Title, mag.Name, c.Name, i.Year, i.Number ORDER BY i.Year ASC, i.Number ASC, mag.Name, MIN(mc.Page)";
        return await conn.QueryAsync<MagazineViewer.Models.ArticleResult>(sql, parameters);
    }

    public async Task<IEnumerable<Magazine>> GetMagazinesAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Magazine>("SELECT * FROM magazine ORDER BY name");
    }

    public async Task<IEnumerable<Model>> SearchModelsAsync(SearchFilters filters)
    {
        using var conn = GetConnection();
        var sql = @"
            SELECT m.*, COALESCE(ac.ArticleCount, 0) AS ArticleCount
            FROM model m
            LEFT JOIN (
                SELECT cm.modelid, COUNT(DISTINCT cm.articleid) AS ArticleCount
                FROM contentmodel cm
                GROUP BY cm.modelid
            ) ac ON m.modelid = ac.modelid
            WHERE 1=1";
        var parameters = new Dapper.DynamicParameters();
        if (!string.IsNullOrEmpty(filters.ModelName))
        {
            sql += " AND m.Name ILIKE @ModelName";
            parameters.Add("ModelName", $"%{filters.ModelName}%");
        }
        if (filters.BustSize.HasValue)
        {
            sql += " AND m.BustSize = @BustSize";
            parameters.Add("BustSize", filters.BustSize);
        }
        if (filters.WaistSize.HasValue)
        {
            sql += " AND m.WaistSize = @WaistSize";
            parameters.Add("WaistSize", filters.WaistSize);
        }
        if (filters.HipSize.HasValue)
        {
            sql += " AND m.HipSize = @HipSize";
            parameters.Add("HipSize", filters.HipSize);
        }
        sql += " ORDER BY m.Name";
        return await conn.QueryAsync<Model>(sql, parameters);
    }

    public async Task<IEnumerable<MagazineContent>> GetModelContentAsync(int modelId)
    {
        using var conn = GetConnection();
        var sql = @"
            SELECT 
                mc.ContentId, mc.IssueId, mc.Page, mc.ArticleId, mc.ImagePath,
                mag.Name as MagazineName, mag.MagazineId, i.Volume, i.Number, i.Year,
                (mag.Name || ' ' || i.Volume || '-' || LPAD(i.Number::text, 2, '0')) as IssueName,
                c.Name as CategoryName, a.Title,
                STRING_AGG(DISTINCT cont.Name, ' | ') as Photographer,
                m.Name as ModelName, cm.Age as ModelAge,
                CASE WHEN m.BustSize IS NOT NULL 
                    THEN CAST(m.BustSize AS VARCHAR) || COALESCE(m.CupSize, '') || '-' || CAST(m.WaistSize AS VARCHAR) || '-' || CAST(m.HipSize AS VARCHAR) 
                    ELSE NULL END as ModelMeasurements
            FROM Content mc
            JOIN Article a ON mc.ArticleId = a.ArticleId
            JOIN Category c ON a.CategoryId = c.CategoryId
            JOIN Issue i ON mc.IssueId = i.IssueId
            JOIN Magazine mag ON i.MagazineId = mag.MagazineId
            JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
            JOIN Model m ON cm.ModelId = m.ModelId
            LEFT JOIN ContentContributor cc ON mc.ContentId = cc.ContentId
            LEFT JOIN Contributor cont ON cc.ContributorId = cont.ContributorId
            WHERE m.ModelId = @ModelId
            GROUP BY mc.ContentId, mc.IssueId, mc.Page, mc.ArticleId, mc.ImagePath,
                mag.Name, mag.MagazineId, i.Volume, i.Number, i.Year,
                c.Name, a.Title, m.Name, cm.Age, m.BustSize, m.CupSize, m.WaistSize, m.HipSize
            ORDER BY i.Year, i.Volume, i.Number, mc.Page";
        return await conn.QueryAsync<MagazineContent>(sql, new { ModelId = modelId });
    }

    public async Task<IEnumerable<Issue>> GetIssuesAsync(int? magazineId = null)
    {
        using var conn = GetConnection();
        var sql = @"
            SELECT i.*, mag.Name as MagazineName, mc.ImagePath as CoverImagePath
            FROM Issue i
            JOIN Magazine mag ON i.MagazineId = mag.MagazineId
            LEFT JOIN (
                SELECT DISTINCT ON (mc.IssueId) mc.IssueId, mc.ImagePath
                FROM Content mc
                JOIN Article a ON mc.ArticleId = a.ArticleId
                JOIN Category c ON a.CategoryId = c.CategoryId
                WHERE c.Name = 'Cover'
                ORDER BY mc.IssueId, mc.Page
            ) mc ON i.IssueId = mc.IssueId";
        if (magazineId.HasValue)
        {
            sql += " WHERE i.MagazineId = @MagazineId";
        }
        sql += " ORDER BY mag.Name, i.Volume, i.Number";
        return await conn.QueryAsync<Issue>(sql, new { MagazineId = magazineId });
    }

    public async Task<IEnumerable<MagazineContent>> GetIssueContentAsync(int issueId)
    {
        using var conn = GetConnection();
        var sql = @"
            SELECT 
                mc.ContentId, mc.IssueId, mc.Page, mc.ArticleId, mc.ImagePath,
                mag.Name as MagazineName, mag.MagazineId, i.Volume, i.Number, i.Year,
                (mag.Name || ' ' || i.Volume || '-' || LPAD(i.Number::text, 2, '0')) as IssueName,
                c.Name as CategoryName, a.Title,
                STRING_AGG(DISTINCT contrib_author.Name, ' | ') as Author,
                STRING_AGG(DISTINCT contrib_photo.Name, ' | ') as Photographer,
                STRING_AGG(DISTINCT contrib_illust.Name, ' | ') as Illustrator,
                STRING_AGG(DISTINCT m.Name, ' | ') as ModelNames,
                MAX(cm.Age) as ModelAge,
                CASE WHEN MAX(m.BustSize) IS NOT NULL 
                    THEN CAST(MAX(m.BustSize) AS VARCHAR) || COALESCE(MAX(m.CupSize), '') || '-' || CAST(MAX(m.WaistSize) AS VARCHAR) || '-' || CAST(MAX(m.HipSize) AS VARCHAR) 
                    ELSE NULL END as ModelMeasurements
            FROM Content mc
            JOIN Article a ON mc.ArticleId = a.ArticleId
            JOIN Category c ON a.CategoryId = c.CategoryId
            JOIN Issue i ON mc.IssueId = i.IssueId
            JOIN Magazine mag ON i.MagazineId = mag.MagazineId
            LEFT JOIN ContentModel cm ON a.ArticleId = cm.ArticleId
            LEFT JOIN Model m ON cm.ModelId = m.ModelId
            LEFT JOIN ContentContributor cc_author ON mc.ContentId = cc_author.ContentId
            LEFT JOIN Contributor contrib_author ON cc_author.ContributorId = contrib_author.ContributorId
            LEFT JOIN ContentContributor cc_photo ON mc.ContentId = cc_photo.ContentId
            LEFT JOIN Contributor contrib_photo ON cc_photo.ContributorId = contrib_photo.ContributorId
            LEFT JOIN ContentContributor cc_illust ON mc.ContentId = cc_illust.ContentId
            LEFT JOIN Contributor contrib_illust ON cc_illust.ContributorId = contrib_illust.ContributorId
            WHERE mc.IssueId = @IssueId
            GROUP BY mc.ContentId, mc.IssueId, mc.Page, mc.ArticleId, mc.ImagePath,
                        mag.Name, mag.MagazineId, i.Volume, i.Number, i.Year,
                        c.Name, a.Title
            ORDER BY mc.Page";
        return await conn.QueryAsync<MagazineContent>(sql, new { IssueId = issueId });
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<string>("SELECT Name FROM Category ORDER BY Name");
    }

    public async Task<Model?> GetModelAsync(int modelId)
    {
        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Model>(
            "SELECT ModelId, Name, YearOfBirth as Age, BustSize, WaistSize, HipSize FROM Model WHERE ModelId = @ModelId",
            new { ModelId = modelId });
    }

    public async Task<Model?> GetModelByNameAsync(string name)
    {
        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Model>(
            "SELECT ModelId, Name, YearOfBirth as Age, BustSize, WaistSize, HipSize FROM Model WHERE Name = @Name",
            new { Name = name });
    }

    public async Task<Dictionary<int, string?>> GetIssueCoverPathsAsync(IEnumerable<int> issueIds)
    {
        using var conn = GetConnection();
        var sql = @"
            SELECT DISTINCT ON (mc.IssueId) mc.IssueId, mc.ImagePath
            FROM Content mc
            JOIN Article a ON mc.ArticleId = a.ArticleId
            JOIN Category c ON a.CategoryId = c.CategoryId
            WHERE c.Name = 'Cover' AND mc.IssueId = ANY(@IssueIds)
            ORDER BY mc.IssueId, mc.Page";
        var results = await conn.QueryAsync<(int IssueId, string? ImagePath)>(sql, new { IssueIds = issueIds.ToArray() });
        return results.ToDictionary(r => r.IssueId, r => r.ImagePath);
    }
    }
}

