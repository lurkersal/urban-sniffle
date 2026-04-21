using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheArchive.Models;

/// <summary>
/// Represents the JSON structure for _index.json files
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class IndexFileJson
{
    public IndexFileJson()
    {
        Metadata = new IndexMetadata();
        Articles = new List<ArticleJson>();
    }

    [JsonPropertyName("metadata")]
    public IndexMetadata Metadata { get; set; }

    [JsonPropertyName("articles")]
    public List<ArticleJson> Articles { get; set; }

    [JsonPropertyName("links")]
    public List<MagazineLink>? Links { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class IndexMetadata
{
    public IndexMetadata()
    {
        Magazine = string.Empty;
        Volume = string.Empty;
        Number = string.Empty;
        Year = string.Empty;
    }

    [JsonPropertyName("magazine")]
    public string Magazine { get; set; }

    [JsonPropertyName("volume")]
    public string Volume { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; }

    [JsonPropertyName("year")]
    public string Year { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class ArticleJson
{
    public ArticleJson()
    {
        Pages = new List<int>();
        Category = string.Empty;
        Title = string.Empty;
    }

    [JsonPropertyName("pages")]
    public List<int> Pages { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("modelNames")]
    public List<string>? ModelNames { get; set; }

    [JsonPropertyName("ages")]
    public List<int?>? Ages { get; set; }

    [JsonPropertyName("contributors")]
    public List<string>? Contributors { get; set; }

    [JsonPropertyName("measurements")]
    public List<string>? Measurements { get; set; }
}

/// <summary>
/// Represents a link from a page in this issue to another magazine issue
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class MagazineLink
{
    public MagazineLink()
    {
        Magazine = string.Empty;
        Volume = string.Empty;
        Issue = string.Empty;
    }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("magazine")]
    public string Magazine { get; set; }

    [JsonPropertyName("volume")]
    public string Volume { get; set; }

    [JsonPropertyName("issue")]
    public string Issue { get; set; }
}

/// <summary>
/// JSON serializer options for index files
/// </summary>
public static class IndexJsonSerializer
{
    public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

