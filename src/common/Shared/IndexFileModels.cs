using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Common.Shared
{
    /// <summary>
    /// Represents the JSON structure for _index.json files
    /// </summary>
    public class IndexFileJson
    {
        [JsonPropertyName("metadata")]
        public IndexMetadata Metadata { get; set; } = new IndexMetadata();

        [JsonPropertyName("articles")]
        public List<ArticleJson> Articles { get; set; } = new List<ArticleJson>();
    }

    public class IndexMetadata
    {
        [JsonPropertyName("magazine")]
        public string Magazine { get; set; } = string.Empty;

        [JsonPropertyName("volume")]
        public string Volume { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("year")]
        public string Year { get; set; } = string.Empty;
    }

    public class ArticleJson
    {
        [JsonPropertyName("pages")]
        public List<int> Pages { get; set; } = new List<int>();

        // Note: Segments are NOT stored in JSON - they are automatically 
        // computed from Pages when loading via RecomputeSegmentsFromPages()

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("modelNames")]
        public List<string>? ModelNames { get; set; }

        [JsonPropertyName("ages")]
        public List<int?>? Ages { get; set; }

        [JsonPropertyName("contributors")]
        public List<string>? Contributors { get; set; }

        [JsonPropertyName("measurements")]
        public List<string>? Measurements { get; set; }
    }

    // SegmentJson class removed - segments are computed, not stored
}

