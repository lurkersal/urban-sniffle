using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Common.Shared
{
    /// <summary>
    /// Handles reading and writing _index.json files
    /// </summary>
    public static class IndexJsonSerializer
    {
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        /// <summary>
        /// Converts ArticleLine objects to JSON format and saves to _index.json
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "JSON types are preserved with DynamicallyAccessedMembers attributes")]
        public static void SaveToJson(string folder, string magazine, string volume, string number, string year, List<ArticleLine> articles, List<MagazineLink>? links = null)
        {
            if (string.IsNullOrWhiteSpace(folder))
                throw new ArgumentException("folder is required", nameof(folder));

            var indexPath = Path.Combine(folder, "_index.json");
            
            var jsonData = new IndexFileJson
            {
                Metadata = new IndexMetadata
                {
                    Magazine = magazine,
                    Volume = volume,
                    Number = number,
                    Year = year
                },
                Articles = articles.Select(ConvertToArticleJson).ToList(),
                Links = links
            };

            var tempPath = indexPath + ".tmp";
            var json = JsonSerializer.Serialize(jsonData, JsonOptions);
            File.WriteAllText(tempPath, json);

            if (File.Exists(indexPath))
            {
                var backupPath = indexPath + "~";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Copy(indexPath, backupPath);
                File.Replace(tempPath, indexPath, null);
            }
            else
            {
                File.Move(tempPath, indexPath);
            }
        }

        /// <summary>
        /// Loads _index.json and converts to ArticleLine objects
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "JSON types are preserved with DynamicallyAccessedMembers attributes")]
        public static (string magazine, string volume, string number, string year, List<ArticleLine> articles, List<MagazineLink>? links) LoadFromJson(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                throw new ArgumentException("folder is required", nameof(folder));

            var indexPath = Path.Combine(folder, "_index.json");
            if (!File.Exists(indexPath))
                throw new FileNotFoundException($"_index.json not found at {indexPath}");

            var json = File.ReadAllText(indexPath);
            var data = JsonSerializer.Deserialize<IndexFileJson>(json, JsonOptions);

            if (data == null)
                throw new InvalidOperationException("Failed to deserialize _index.json");

            var articles = data.Articles.Select(ConvertToArticleLine).ToList();

            return (
                data.Metadata.Magazine,
                data.Metadata.Volume,
                data.Metadata.Number,
                data.Metadata.Year,
                articles,
                data.Links
            );
        }

        /// <summary>
        /// Checks if _index.json exists in the folder
        /// </summary>
        public static bool JsonExists(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return false;
            return File.Exists(Path.Combine(folder, "_index.json"));
        }

        private static ArticleJson ConvertToArticleJson(ArticleLine article)
        {
            return new ArticleJson
            {
                Pages = article.Pages.ToList(),
                // Segments are NOT serialized - they're computed from Pages
                Category = article.Category,
                Title = article.Title,
                ModelNames = article.ModelNames?.Count > 0 ? new List<string>(article.ModelNames) : null,
                Ages = article.Ages?.Count > 0 ? new List<int?>(article.Ages) : null,
                Contributors = article.Contributors?.Count > 0 ? new List<string>(article.Contributors) : null,
                Measurements = article.Measurements?.Count > 0 ? new List<string>(article.Measurements) : null,
                ThumbnailPage = article.ThumbnailPage
            };
        }

        private static ArticleLine ConvertToArticleLine(ArticleJson json)
        {
            var article = new ArticleLine
            {
                Category = json.Category,
                Title = json.Title
            };

            // Set pages - this will automatically trigger RecomputeSegmentsFromPages()
            // via the Pages property setter in ArticleLine
            if (json.Pages.Count > 0)
            {
                article.Pages = json.Pages;
            }

            // Segments are automatically computed by ArticleLine.Pages setter
            // No need to manually set them here

            // Set optional fields - directly assign List properties
            if (json.ModelNames != null && json.ModelNames.Count > 0)
            {
                article.ModelNames = new System.Collections.Generic.List<string>(json.ModelNames);
            }

            if (json.Ages != null && json.Ages.Count > 0)
            {
                article.Ages = new System.Collections.Generic.List<int?>(json.Ages);
            }

            if (json.Contributors != null && json.Contributors.Count > 0)
            {
                article.Contributors = new System.Collections.Generic.List<string>(json.Contributors);
            }

            if (json.Measurements != null && json.Measurements.Count > 0)
            {
                article.Measurements = new System.Collections.Generic.List<string>(json.Measurements);
            }

            if (json.ThumbnailPage.HasValue)
            {
                article.ThumbnailPage = json.ThumbnailPage;
            }

            return article;
        }
    }
}

