using Dapper;
using Npgsql;

namespace TheArchive.Services;

public class ArchiveStatistics
{
    private readonly string _connectionString;

    public ArchiveStatistics(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ArchiveStats> GetStatisticsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var stats = new ArchiveStats();
        
        // Get counts
        stats.TotalMagazines = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM magazine");
        stats.TotalIssues = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM issue");
        stats.TotalArticles = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM content");
        stats.TotalModels = await connection.QuerySingleAsync<int>("SELECT COUNT(DISTINCT model_id) FROM content_model WHERE model_id IS NOT NULL");
        stats.TotalPhotographers = await connection.QuerySingleAsync<int>("SELECT COUNT(DISTINCT photographer) FROM content WHERE photographer IS NOT NULL");
        
        // Get detailed data
        stats.RecentIssues = await GetRecentIssuesAsync(connection);
        stats.TopModels = await GetTopModelsAsync(connection);
        stats.CategoryCounts = await GetCategoryDistributionAsync(connection);
        
        return stats;
    }

    private async Task<List<RecentIssue>> GetRecentIssuesAsync(NpgsqlConnection connection)
    {
        var sql = @"
            SELECT 
                i.issue_id as IssueId,
                m.name as MagazineName,
                i.year,
                i.number,
                COUNT(c.content_id) as ArticleCount
            FROM issue i
            JOIN magazine m ON i.magazine_id = m.magazine_id
            LEFT JOIN content c ON i.issue_id = c.issue_id
            GROUP BY i.issue_id, m.name, i.year, i.number
            ORDER BY i.year DESC, i.number DESC
            LIMIT 5";
        
        var results = await connection.QueryAsync(sql);
        return results.Select(r => new RecentIssue
        {
            IssueId = r.IssueId,
            MagazineName = r.MagazineName,
            Year = r.year,
            Number = r.number,
            ArticleCount = r.ArticleCount
        }).ToList();
    }

    private async Task<List<TopModel>> GetTopModelsAsync(NpgsqlConnection connection)
    {
        var sql = @"
            SELECT 
                m.model_id as ModelId,
                m.name as Name,
                COUNT(DISTINCT cm.content_id) as AppearanceCount
            FROM model m
            JOIN content_model cm ON m.model_id = cm.model_id
            GROUP BY m.model_id, m.name
            ORDER BY AppearanceCount DESC
            LIMIT 5";
        
        var results = await connection.QueryAsync(sql);
        return results.Select(r => new TopModel
        {
            ModelId = r.ModelId,
            Name = r.Name,
            AppearanceCount = r.AppearanceCount
        }).ToList();
    }

    private async Task<Dictionary<string, int>> GetCategoryDistributionAsync(NpgsqlConnection connection)
    {
        var sql = @"
            SELECT 
                cat.name as CategoryName,
                COUNT(*) as Count
            FROM content c
            JOIN category cat ON c.category_id = cat.category_id
            GROUP BY cat.name
            ORDER BY Count DESC";
        
        var results = await connection.QueryAsync(sql);
        return results.ToDictionary(
            r => (string)r.CategoryName,
            r => (int)r.Count
        );
    }
}

public class ArchiveStats
{
    public int TotalMagazines { get; set; }
    public int TotalIssues { get; set; }
    public int TotalArticles { get; set; }
    public int TotalModels { get; set; }
    public int TotalPhotographers { get; set; }
    public List<RecentIssue> RecentIssues { get; set; } = new();
    public List<TopModel> TopModels { get; set; } = new();
    public Dictionary<string, int> CategoryCounts { get; set; } = new();
}

public class RecentIssue
{
    public int IssueId { get; set; }
    public string MagazineName { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Number { get; set; } = string.Empty;
    public int ArticleCount { get; set; }
}

public class TopModel
{
    public int ModelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AppearanceCount { get; set; }
}


