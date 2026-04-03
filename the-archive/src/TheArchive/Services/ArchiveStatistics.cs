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
        stats.TotalModels = await connection.QuerySingleAsync<int>("SELECT COUNT(DISTINCT modelid) FROM contentmodel WHERE modelid IS NOT NULL");
        stats.TotalPhotographers = await connection.QuerySingleAsync<int>("SELECT COUNT(DISTINCT contributorid) FROM contentcontributor");
        
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
                i.issueid,
                m.name as magazinename,
                i.year,
                i.number,
                COUNT(c.contentid) as articlecount
            FROM issue i
            JOIN magazine m ON i.magazineid = m.magazineid
            LEFT JOIN content c ON i.issueid = c.issueid
            GROUP BY i.issueid, m.name, i.year, i.number
            ORDER BY i.year DESC, i.number DESC
            LIMIT 5";
        
        var results = await connection.QueryAsync(sql);
        return results.Select(r => new RecentIssue
        {
            IssueId = (int)r.issueid,
            MagazineName = (string)r.magazinename ?? string.Empty,
            Year = (int)r.year,
            Number = r.number.ToString() ?? string.Empty,
            ArticleCount = (long)r.articlecount
        }).ToList();
    }

    private async Task<List<TopModel>> GetTopModelsAsync(NpgsqlConnection connection)
    {
        var sql = @"
            SELECT 
                m.modelid,
                m.name,
                COUNT(DISTINCT cm.articleid) as appearancecount
            FROM model m
            JOIN contentmodel cm ON m.modelid = cm.modelid
            GROUP BY m.modelid, m.name
            ORDER BY appearancecount DESC
            LIMIT 5";
        
        var results = await connection.QueryAsync(sql);
        return results.Select(r => new TopModel
        {
            ModelId = (int)r.modelid,
            Name = (string)r.name ?? string.Empty,
            AppearanceCount = (int)(long)r.appearancecount
        }).ToList();
    }

    private async Task<Dictionary<string, int>> GetCategoryDistributionAsync(NpgsqlConnection connection)
    {
        var sql = @"
            SELECT 
                cat.name as categoryname,
                COUNT(*) as count
            FROM content c
            JOIN article a ON c.articleid = a.articleid
            JOIN category cat ON a.categoryid = cat.categoryid
            GROUP BY cat.name
            ORDER BY count DESC";
        
        var results = await connection.QueryAsync(sql);
        return results.ToDictionary(
            r => (string)r.categoryname ?? string.Empty,
            r => (int)(long)r.count
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
    public long ArticleCount { get; set; }
}

public class TopModel
{
    public int ModelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AppearanceCount { get; set; }
}


