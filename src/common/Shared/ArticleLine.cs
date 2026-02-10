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
                    _category = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Category)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayTitle)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CategoryDisplay)));
                    Validate();
                    // Category affects which underlying list is used for Contributor0 (authors vs photographers)
                    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Contributor0))); } catch { }
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

                // Rebuild closed segments from the canonical Pages list so the UI reflects any changes
                // (e.g., when an active segment is ended and Pages are updated to include the range).
                RecomputeSegmentsFromPages();
            }
        }

        private void RecomputeSegmentsFromPages()
        {
            try
            {
                var pages = Pages ?? new List<int>();
                var newSegs = new List<Segment>();
                if (pages != null && pages.Count > 0)
                {
                    var sorted = pages.OrderBy(p => p).ToList();
                    int i = 0;
                    while (i < sorted.Count)
                    {
                        int start = sorted[i];
                        int end = start;
                        i++;
                        while (i < sorted.Count && sorted[i] == end + 1)
                        {
                            end = sorted[i];
                            i++;
                        }
                        // Closed segment (end set)
                        newSegs.Add(new Segment(start, end));
                    }
                }
                // Update the existing ObservableCollection so UI bindings observing collection changes update immediately
                Segments.Clear();
                foreach (var s in newSegs) Segments.Add(s);
                try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveSegment))); } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.RecomputeSegmentsFromPages: notify ActiveSegment", ex); }
            }
            catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.RecomputeSegmentsFromPages: outer", ex); }
        }

        public bool HasPageNumberError { get; set; }
        public List<string> ModelNames { get; set; } = new();
        public int? Age { get; set; }
        public List<int?> Ages { get; set; } = new();
        public List<string> Contributors { get; set; } = new();
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
        public System.Collections.ObjectModel.ObservableCollection<Segment> Segments { get; set; } = new System.Collections.ObjectModel.ObservableCollection<Segment>();
        // The segment that was most recently modified (added/ended/reopened) on this article. Not persisted.
        private Segment? _lastModifiedSegment;
        public Segment? LastModifiedSegment
        {
            get => _lastModifiedSegment;
            set
            {
                if (_lastModifiedSegment != value)
                {
                    _lastModifiedSegment = value;
                    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastModifiedSegment))); } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.LastModifiedSegment: notify", ex); }
                }
            }
        }

        public ArticleLine()
        {
            // Ensure we observe collection changes so ActiveSegment updates when segments are added/removed
            try
            {
                Segments.CollectionChanged += Segments_CollectionChanged;
            }
            catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.ctor: subscribe Segments.CollectionChanged", ex); }
        }

        // Provide a safe way for external code to request a property change notification
        // (can't invoke the event from outside the declaring class).
        public void NotifyPropertyChanged(string propertyName)
        {
            try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.NotifyPropertyChanged", ex); }
        }

        private void Segments_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.NewItems != null)
                {
                    foreach (var ni in e.NewItems)
                    {
                        if (ni is Segment s)
                        {
                            try { s.PropertyChanged += Segment_PropertyChanged; } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.Segments_CollectionChanged: attach Segment_PropertyChanged", ex); }
                             // If the new segment is active or was created as part of an add-operation (WasNew),
                             // consider it as the last-modified so ActiveSegmentDisplay can show it.
                             if (s.IsActive || s.WasNew)
                             {
                                try { LastModifiedSegment = s; } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.Segments_CollectionChanged: set LastModifiedSegment", ex); }
                             }
                        }
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (var oi in e.OldItems)
                    {
                        if (oi is Segment s)
                        {
                            try { s.PropertyChanged -= Segment_PropertyChanged; } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.Segments_CollectionChanged: detach Segment_PropertyChanged", ex); }
                        }
                    }
                }
            }
            catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.Segments_CollectionChanged: outer", ex); }
             // Notify that ActiveSegment and Segments/FormattedCardText may have changed
            try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveSegment))); } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.Segments_CollectionChanged: notify ActiveSegment", ex); }
            try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText))); } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.Segments_CollectionChanged: notify FormattedCardText", ex); }
         }

        private void Segment_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(Segment.IsActive) || e.PropertyName == nameof(Segment.End) || e.PropertyName == nameof(Segment.Start))
                {
                    // Mark this segment as last-modified when its End or IsActive changes
                    try { LastModifiedSegment = sender as Segment; } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.Segment_PropertyChanged: set LastModifiedSegment", ex); }
                    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveSegment))); } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.Segment_PropertyChanged: notify ActiveSegment", ex); }
                    try { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText))); } catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.Segment_PropertyChanged: notify FormattedCardText", ex); }
                }
            }
            catch (Exception ex) { Common.Shared.Logger.LogException("ArticleLine.Segment_PropertyChanged: outer", ex); }
        }

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

        // Unified contributor convenience property (first contributor)
        public string Contributor0
        {
            get => Contributors.Count > 0 ? Contributors[0] : string.Empty;
            set
            {
                if (Contributors.Count == 0) Contributors.Add(string.Empty);
                if (Contributors[0] != value)
                {
                    Contributors[0] = value ?? string.Empty;
                    // Keep backward-compatible behaviour through Contributors only
                    // (no separate Photographers/Authors lists maintained)
                     
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Contributors)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Contributor0)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                }
            }
        }

        // Photographer0 proxies to Contributors for backward compatibility
        public string Photographer0
        {
            get => Contributors.Count > 0 ? Contributors[0] : string.Empty;
            set
            {
                if (Contributors.Count == 0) Contributors.Add(string.Empty);
                if (Contributors[0] != value)
                {
                    Contributors[0] = value ?? string.Empty;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Contributor0)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Photographer0)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormattedCardText)));
                }
            }
        }

        // Author0 proxies to Contributors for backward compatibility
        public string Author0
        {
            get => Contributors.Count > 0 ? Contributors[0] : string.Empty;
            set
            {
                if (Contributors.Count == 0) Contributors.Add(string.Empty);
                if (Contributors[0] != value)
                {
                    Contributors[0] = value ?? string.Empty;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Contributor0)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Author0)));
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

            // Cartoons: rename photographer to cartoonist (use Contributors if available)
            if (cat == "cartoons")
                return $"{categoryText}\n{pagesText}\nTitle: {Title}\nCartoonist: {string.Join(", ", Contributors)}";

            // Model and Cover: show photographer, model, age, measurements (use Contributors if populated)
            if (cat == "model" || cat == "cover")
                return $"{categoryText}\n{pagesText}\nModel: {string.Join(", ", ModelNames)}\nAge: {string.Join(", ", Ages.Where(a => a.HasValue).Select(a => a.GetValueOrDefault().ToString()))}\nPhotographer: {string.Join(", ", Contributors)}\nMeasurements: {string.Join(", ", Measurements)}";

            // Feature, Fiction, Review, Humour: show contributors as Author
            if (cat == "feature" || cat == "fiction" || cat == "review" || cat == "humour" || cat == "humor")
                return $"{categoryText}\n{pagesText}\nTitle: {Title}\nAuthor: {string.Join(", ", Contributors)}";

            // Photographer category: show photographer and title (contributors if present)
            if (cat == "photographer")
                return $"{categoryText}\n{pagesText}\nPhotographer: {string.Join(", ", Contributors)}\nTitle: {Title}";

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
            // Category-specific validation: Model/Cover measurements are optional; if provided, validate format
            HasMeasurementsError = false;
            var cat = (Category ?? string.Empty).Trim().ToLowerInvariant();
            if (cat == "model" || cat == "cover")
            {
                // Use Measurements0 as the user-editable input (first measurement string)
                if (!string.IsNullOrWhiteSpace(Measurements0))
                {
                    // If the user provided a measurements string, validate its format
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
                else
                {
                    // Measurements left empty: optional -> clear any previous message
                    MeasurementsErrorMessage = null;
                    HasMeasurementsError = false;
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
