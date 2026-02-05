using System.Collections.Generic;

namespace Common.Shared
{
    public class ArticleLine : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? Category : Title;
        /// <summary>
        /// Property for XAML binding: returns formatted string for display in the card, with fields shown depending on Category.
        /// </summary>
        public string FormattedCardText => GetFormattedCardText();
        public List<int> Pages { get; set; } = new();
        public bool HasPageNumberError { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<string> ModelNames { get; set; } = new();
        public int? Age { get; set; }
        public List<int?> Ages { get; set; } = new();
        public List<string> Photographers { get; set; } = new();
        public List<string> Authors { get; set; } = new();
        public List<string> Illustrators { get; set; } = new();
        public string ModelSize { get; set; } = string.Empty;
        public List<string> Measurements { get; set; } = new();
        public int? BustSize { get; set; }
        public int? WaistSize { get; set; }
        public int? HipSize { get; set; }
        public string? CupSize { get; set; }
        public List<int?> BustSizes { get; set; } = new();
        public List<int?> WaistSizes { get; set; } = new();
        public List<int?> HipSizes { get; set; } = new();
        public List<string?> CupSizes { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
        public bool HasValidationError => ValidationErrors.Count > 0;
        public bool WasAutoInserted { get; set; }

        /// <summary>
        /// Returns a formatted string for display in the card, with fields shown depending on Category.
        /// </summary>
        public string GetFormattedCardText()
        {
            // Example logic: adjust as needed for your real categories/fields
            string pagesText = $"Pages: {string.Join(", ", Pages)}";
            string categoryText = $"Category: {Category}";
            var cat = Category?.ToLowerInvariant();
            // Index/Contents: only show category and page
            if (cat == "index" || cat == "contents")
                return $"{categoryText}\n{pagesText}";

            // Editorial: do not show photographer
            if (cat == "editorial")
                return $"{categoryText}\n{pagesText}\nTitle: {Title}";

            // Cartoons: rename photographer to cartoonist
            if (cat == "cartoons")
                return $"{categoryText}\n{pagesText}\nTitle: {Title}\nCartoonist: {string.Join(", ", Photographers)}";

            // Model and Cover: show photographer, model, age, measurements
            if (cat == "model" || cat == "cover")
                return $"{categoryText}\n{pagesText}\nModel: {string.Join(", ", ModelNames)}\nAge: {string.Join(", ", Ages.Where(a => a.HasValue).Select(a => a.Value.ToString()))}\nPhotographer: {string.Join(", ", Photographers)}\nMeasurements: {string.Join(", ", Measurements)}";

            // Feature, Fiction, Review: rename photographer as author
            if (cat == "feature" || cat == "fiction" || cat == "review")
                return $"{categoryText}\n{pagesText}\nTitle: {Title}\nAuthor: {string.Join(", ", Photographers)}";

            // Photographer category: show photographer and title
            if (cat == "photographer")
                return $"{categoryText}\n{pagesText}\nPhotographer: {string.Join(", ", Photographers)}\nTitle: {Title}";

            // Illustration: show illustrator and title
            if (cat == "illustration")
                return $"{categoryText}\n{pagesText}\nIllustrator: {string.Join(", ", Illustrators)}\nTitle: {Title}";

            // Default: show category, page, title
            return $"{categoryText}\n{pagesText}\nTitle: {Title}";
        }
    }
}