using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexEditor.Shared
{
    /// <summary>
    /// Article categories extracted from database schema.
    /// These values must match the Category table in schema_postgres.sql.
    /// </summary>
    public enum ArticleCategory
    {
        Group,
        Cover,
        Index,
        Editorial,
        Cartoons,
        Letters,
        Wives,
        Model,
        Pinup,
        Fiction,
        Feature,
        Humour,
        Motoring,
        Travel,
        Review,
        Illustrations,
        Interview
    }

    /// <summary>
    /// Helper class to work with ArticleCategory enum.
    /// </summary>
    public static class ArticleCategoryHelper
    {
        private static readonly List<string> _allCategories;

        static ArticleCategoryHelper()
        {
            // Get all enum values as strings
            _allCategories = Enum.GetValues(typeof(ArticleCategory))
                .Cast<ArticleCategory>()
                .Select(c => c.ToString())
                .OrderBy(c => c)
                .ToList();
        }

        /// <summary>
        /// Gets all category names as a sorted list of strings.
        /// </summary>
        public static List<string> GetAllCategories()
        {
            return new List<string>(_allCategories);
        }

        /// <summary>
        /// Tries to parse a string to an ArticleCategory enum value.
        /// </summary>
        public static bool TryParse(string? categoryName, out ArticleCategory category)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                category = default;
                return false;
            }

            return Enum.TryParse(categoryName, true, out category);
        }

        /// <summary>
        /// Gets the string name of a category.
        /// </summary>
        public static string GetName(ArticleCategory category)
        {
            return category.ToString();
        }

        /// <summary>
        /// Checks if a string is a valid category name.
        /// </summary>
        public static bool IsValidCategory(string? categoryName)
        {
            return TryParse(categoryName, out _);
        }
    }
}

