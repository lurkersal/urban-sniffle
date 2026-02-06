using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Common.Shared
{
    public class ArticleLine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        private string _category = string.Empty;
        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    var old = _category;
                    _category = value;
                    System.Console.WriteLine($"[TRACE] ArticleLine.Category changed from '{old}' to '{_category}' for article Title='{Title}'");
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Category)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayTitle)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CategoryDisplay)));
                    Validate();
                }
            }
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayTitle)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                }
            }
        }

        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? (string.IsNullOrWhiteSpace(Category) ? $"missing Title" : Category) : Title;
        public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "missing Category" : Category;
        public string PagesDisplay => (Pages == null || Pages.Count == 0) ? "missing Pages" : string.Join(", ", Pages);

        /// <summary>
        /// Property for XAML binding: returns formatted string for display in the card, with fields shown depending on Category.
        /// </summary>
        public string FormattedCardText => GetFormattedCardText();

        private List<int> _pages = new();
        public List<int> Pages
        {
            get => _pages;
            set
            {
                _pages = value ?? new List<int>();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pages)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PagesText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PagesDisplay)));
                Validate();
            }
        }

        public bool HasPageNumberError { get; set; }
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
        public List<Segment> Segments { get; set; } = new();
        public Segment? ActiveSegment => Segments.FirstOrDefault(s => s.IsActive);
        public bool HasValidationError { get; private set; }
        public bool HasMeasurementsError { get; private set; }
        public string? MeasurementsErrorMessage { get; private set; }
        public bool WasAutoInserted { get; set; }

        private bool _wasAutoHighlighted;
        public bool WasAutoHighlighted
        {
            get => _wasAutoHighlighted;
            set
            {
                if (_wasAutoHighlighted != value)
                {
                    _wasAutoHighlighted = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WasAutoHighlighted)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                }
            }
        }

        // New Notes property (for editor)
        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Notes)));
                }
            }
        }

        // PagesText property for user-friendly editing (e.g. "2|3-4")
        public string PagesText
        {
            get => string.Join("|", PagesToSegments(Pages));
            set
            {
                var parsed = ParsePageText(value, out bool hasError);
                Pages = parsed;
                HasPageNumberError = hasError;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PagesText)));
                Validate();
            }
        }

        // Helper properties so bindings to [0] are easier to two-way bind and notify
        public string ModelName0
        {
            get => ModelNames.Count > 0 ? ModelNames[0] : string.Empty;
            set
            {
                if (ModelNames.Count == 0) ModelNames.Add(string.Empty);
                if (ModelNames[0] != value)
                {
                    ModelNames[0] = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ModelNames)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ModelName0)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                }
            }
        }

        public string Photographer0
        {
            get => Photographers.Count > 0 ? Photographers[0] : string.Empty;
            set
            {
                if (Photographers.Count == 0) Photographers.Add(string.Empty);
                if (Photographers[0] != value)
                {
                    Photographers[0] = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Photographers)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Photographer0)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                }
            }
        }

        public int? Age0
        {
            get => Ages.Count > 0 ? Ages[0] : null;
            set
            {
                if (Ages.Count == 0) Ages.Add(null);
                if (Ages[0] != value)
                {
                    Ages[0] = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Ages)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Age0)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                }
            }
        }

        public string Measurements0
        {
            get => Measurements.Count > 0 ? Measurements[0] : string.Empty;
            set
            {
                if (Measurements.Count == 0) Measurements.Add(string.Empty);
                // Normalize user input to canonical form before storing (removes cm, normalizes dashes, trims, uppercases cup letters)
                var normalized = MeasurementsValidator.NormalizeMeasurement(value);
                if (Measurements[0] != normalized)
                {
                    Measurements[0] = normalized;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Measurements)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Measurements0)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                    // Re-validate immediately when the user edits the measurements field
                    Validate();
                }
            }
        }

        private IEnumerable<string> PagesToSegments(List<int> pages)
        {
            if (pages == null || pages.Count == 0) return new string[0];
            var segments = new List<string>();
            int i = 0;
            while (i < pages.Count)
            {
                int start = pages[i];
                int end = start;
                i++;
                while (i < pages.Count && pages[i] == end + 1)
                {
                    end = pages[i];
                    i++;
                }
                if (start == end) segments.Add(start.ToString());
                else segments.Add($"{start}-{end}");
            }
            return segments;
        }

        private List<int> ParsePageText(string text, out bool hasError)
        {
            var result = new List<int>();
            hasError = false;
            if (string.IsNullOrWhiteSpace(text))
            {
                hasError = true;
                return result;
            }
            var parts = text.Split('|');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Contains("-"))
                {
                    var range = trimmed.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end) && start <= end)
                    {
                        for (int p = start; p <= end; p++) result.Add(p);
                    }
                    else
                    {
                        hasError = true;
                    }
                }
                else if (int.TryParse(trimmed, out int single))
                {
                    result.Add(single);
                }
                else
                {
                    hasError = true;
                }
            }
            return result;
        }

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

        /// <summary>
        /// Validate required fields and set ValidationErrors / HasValidationError accordingly.
        /// </summary>
        public void Validate()
        {
            var errors = new List<string>();
            if (Pages == null || Pages.Count == 0)
                errors.Add("Pages");
            if (string.IsNullOrWhiteSpace(Category))
                errors.Add("Category");
            // Category-specific validation: Model/Cover require measurements
            HasMeasurementsError = false;
            var cat = (Category ?? string.Empty).Trim().ToLowerInvariant();
            if (cat == "model" || cat == "cover")
            {
                // Use Measurements0 as the user-editable input (first measurement string)
                if (string.IsNullOrWhiteSpace(Measurements0))
                {
                    errors.Add("Measurements");
                    HasMeasurementsError = true;
                    MeasurementsErrorMessage = "Measurements are required for Model/Cover";
                }
                else
                {
                    if (!Common.Shared.MeasurementsValidator.TryParseMeasurements(Measurements0, out var b, out var cup, out var w, out var h, out var mErr))
                    {
                        errors.Add("Measurements");
                        HasMeasurementsError = true;
                        MeasurementsErrorMessage = mErr ?? "Invalid measurements format";
                    }
                    else
                    {
                        MeasurementsErrorMessage = null;
                    }
                }
            }
            // You can add more category-specific rules here later.

            ValidationErrors = errors;
            var had = HasValidationError;
            HasValidationError = ValidationErrors.Count > 0;
            if (had != HasValidationError)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasValidationError)));

            // Raise per-field notifications so UI can update
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationErrors)));
            // Raise convenience boolean properties for bindings
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasPagesError)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasCategoryError)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMeasurementsError)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MeasurementsErrorMessage)));
        }

        public bool HasFieldError(string fieldName)
        {
            return ValidationErrors != null && ValidationErrors.Contains(fieldName);
        }

        // Convenience boolean properties for XAML bindings
        public bool HasPagesError => HasFieldError("Pages");
        public bool HasCategoryError => HasFieldError("Category");
    }
}
