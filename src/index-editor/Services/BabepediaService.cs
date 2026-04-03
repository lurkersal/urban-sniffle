using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using IndexEditor.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service to check if a model has a page on babepedia.com
/// </summary>
public class BabepediaService
{
    private static readonly HttpClient _httpClient = new HttpClient();

    static BabepediaService()
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; IndexEditor/1.0)");
    }

    /// <summary>
    /// Checks if a model has a page on babepedia.com
    /// </summary>
    /// <param name="modelName">The model name to search for</param>
    /// <returns>True if a page exists, false otherwise</returns>
    public static async Task<(bool exists, string url)> CheckModelPageAsync(string modelName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return (false, string.Empty);
            }

            // Clean up the model name for URL
            // Replace spaces with underscores, remove special characters
            var cleanName = modelName.Trim()
                .Replace(" ", "_")
                .Replace("'", "")
                .Replace("\"", "");

            // Babepedia URL format: https://www.babepedia.com/babe/Model_Name
            var requestedUrl = $"https://www.babepedia.com/babe/{Uri.EscapeDataString(cleanName)}";

            DebugLogger.Debug($"BabepediaService: Checking URL: {requestedUrl}");

            var response = await _httpClient.GetAsync(requestedUrl);

            // Babepedia redirects from /babe/Name to /search/Name when the model doesn't exist
            // We need to check if the final URL after redirects is different from what we requested
            var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? requestedUrl;
            
            DebugLogger.Debug($"BabepediaService: Requested: {requestedUrl}");
            DebugLogger.Debug($"BabepediaService: Final URL: {finalUrl}");

            // Check if we got redirected to a search page
            bool wasRedirectedToSearch = finalUrl.Contains("/search/", StringComparison.OrdinalIgnoreCase);
            
            // The page exists only if:
            // 1. The request was successful (200 OK)
            // 2. We were NOT redirected to a search page
            bool exists = response.IsSuccessStatusCode && !wasRedirectedToSearch;

            DebugLogger.Debug($"BabepediaService: {modelName} -> Status: {response.StatusCode}, Redirected to search: {wasRedirectedToSearch}, Exists: {exists}");

            return (exists, exists ? finalUrl : string.Empty);
        }
        catch (TaskCanceledException)
        {
            DebugLogger.Debug($"BabepediaService: Timeout checking {modelName}");
            return (false, string.Empty);
        }
        catch (Exception ex)
        {
            DebugLogger.Warning($"BabepediaService: Error checking {modelName}");
            DebugLogger.LogException($"BabepediaService: Error checking {modelName}", ex);
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// Checks babepedia for the first model name in an article
    /// </summary>
    public static async Task<(bool exists, string modelName, string url)> CheckArticleModelAsync(Common.Shared.ArticleLine article)
    {
        try
        {
            if (article == null)
            {
                return (false, string.Empty, string.Empty);
            }

            // Only check Model and Cover categories
            if (article.Category != "Model" && article.Category != "Cover")
            {
                return (false, string.Empty, string.Empty);
            }

            // Get the first model name
            var modelName = article.ModelNames?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return (false, string.Empty, string.Empty);
            }

            var (exists, url) = await CheckModelPageAsync(modelName);
            return (exists, modelName ?? string.Empty, url);
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("BabepediaService: CheckArticleModelAsync", ex);
            return (false, string.Empty, string.Empty);
        }
    }
}




