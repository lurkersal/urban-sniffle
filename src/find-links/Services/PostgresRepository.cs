using Npgsql;
using System.Collections.Generic;

namespace FindLinks.Services;

public class PostgresRepository : IDatabaseRepository
{
    public bool GetLinkScanPerformed(int issueId)
    {
        using var cmd = new NpgsqlCommand("SELECT LinkScanPerformed FROM Issue WHERE IssueId = @issueid", _connection);
        cmd.Parameters.AddWithValue("issueid", issueId);
        var result = cmd.ExecuteScalar();
        return result != null && (bool)result;
    }

    public void SetLinkScanPerformed(int issueId, bool performed)
    {
        using var cmd = new NpgsqlCommand("UPDATE Issue SET LinkScanPerformed = @performed WHERE IssueId = @issueid", _connection);
        cmd.Parameters.AddWithValue("performed", performed);
        cmd.Parameters.AddWithValue("issueid", issueId);
        int affected = cmd.ExecuteNonQuery();
        Console.WriteLine($"DEBUG: SetLinkScanPerformed({issueId}, {performed}) affected {affected} row(s)");
    }
    public List<(int volume, int number)> GetAllIssuesForMagazine(int magazineId)
    {
        var issues = new List<(int volume, int number)>();
        using var cmd = new NpgsqlCommand("SELECT Volume, Number FROM Issue WHERE MagazineId = @mid ORDER BY Volume, Number", _connection);
        cmd.Parameters.AddWithValue("mid", magazineId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            issues.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }
        return issues;
    }

    public List<int> GetNumbersForMagazineVolume(int magazineId, int volume)
    {
        var numbers = new List<int>();
        using var cmd = new NpgsqlCommand("SELECT Number FROM Issue WHERE MagazineId = @mid AND Volume = @vol ORDER BY Number", _connection);
        cmd.Parameters.AddWithValue("mid", magazineId);
        cmd.Parameters.AddWithValue("vol", volume);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            numbers.Add(reader.GetInt32(0));
        }
        return numbers;
    }
    public bool IssueLinksExistForIssue(int issueId)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM IssueLink il JOIN Content mc ON il.ContentId = mc.ContentId WHERE mc.IssueId = @issueid LIMIT 1",
            _connection);
        cmd.Parameters.AddWithValue("issueid", issueId);
        var result = cmd.ExecuteScalar();
        return result != null;
    }
    public Dictionary<int, string?> GetPageImagePathsForIssue(int issueId)
    {
        var result = new Dictionary<int, string?>();
        using var cmd = new NpgsqlCommand(
            "SELECT Page, ImagePath FROM Content WHERE IssueId = @issueid",
            _connection);
        cmd.Parameters.AddWithValue("issueid", issueId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int page = reader.GetInt32(0);
            string? imagePath = reader.IsDBNull(1) ? null : reader.GetString(1);
            result[page] = imagePath;
        }
        return result;
    }
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

    public List<int> GetPagesForIssue(int issueId)
    {
        var pages = new List<int>();
        using var cmd = new NpgsqlCommand(
            "SELECT DISTINCT Page FROM Content WHERE IssueId = @issueid ORDER BY Page",
            _connection);
        cmd.Parameters.AddWithValue("issueid", issueId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            pages.Add(reader.GetInt32(0));
        }
        return pages;
    }

    public Dictionary<int, string> GetPageCategoriesForIssue(int issueId)
    {
        var result = new Dictionary<int, string>();
                using var cmd = new NpgsqlCommand(
                        @"SELECT mc.Page, c.Name as Category
                            FROM Content mc
                            JOIN Article a ON mc.ArticleId = a.ArticleId
                            JOIN Category c ON a.CategoryId = c.CategoryId
                            WHERE mc.IssueId = @issueid",
                        _connection);
        cmd.Parameters.AddWithValue("issueid", issueId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int page = reader.GetInt32(0);
            string category = reader.GetString(1);
            result[page] = category;
        }
        return result;
    }
    public void InsertIssueLinks(int contentId, IEnumerable<(int magazineId, int volume, int number, int page)> linkedIssues)
    {
        foreach (var (magazineId, volume, number, page) in linkedIssues.Distinct())
        {
            // Check if the link already exists (now also check page)
            using var checkCmd = new NpgsqlCommand(
                "SELECT 1 FROM IssueLink WHERE ContentId = @contentid AND LinkedMagazineId = @magazineid AND LinkedVolume = @volume AND LinkedNumber = @number AND Page = @page",
                _connection);
            checkCmd.Parameters.AddWithValue("contentid", contentId);
            checkCmd.Parameters.AddWithValue("magazineid", magazineId);
            checkCmd.Parameters.AddWithValue("volume", volume);
            checkCmd.Parameters.AddWithValue("number", number);
            checkCmd.Parameters.AddWithValue("page", page);
            var exists = checkCmd.ExecuteScalar();
            if (exists == null)
            {
                using var insertCmd = new Npgsql.NpgsqlCommand(
                    "INSERT INTO IssueLink (ContentId, LinkedMagazineId, LinkedVolume, LinkedNumber, Page) VALUES (@contentid, @magazineid, @volume, @number, @page)",
                    _connection);
                insertCmd.Parameters.AddWithValue("contentid", contentId);
                insertCmd.Parameters.AddWithValue("magazineid", magazineId);
                insertCmd.Parameters.AddWithValue("volume", volume);
                insertCmd.Parameters.AddWithValue("number", number);
                insertCmd.Parameters.AddWithValue("page", page);
                insertCmd.ExecuteNonQuery();
            }
        }
    }
}
