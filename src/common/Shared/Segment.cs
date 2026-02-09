namespace Common.Shared
{
    using System.ComponentModel;

    public class Segment : INotifyPropertyChanged
    {
        private int _start;
        private int? _end;
        private bool _isHighlighted;
        private int? _originalEnd;
        private bool _wasNew;
        private int? _currentPreviewEnd;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Start
        {
            get => _start;
            set
            {
                if (_start != value)
                {
                    _start = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Start)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Display)));
                }
            }
        }

        public int? End
        {
            get => _end;
            set
            {
                if (_end != value)
                {
                    _end = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(End)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Display)));
                }
            }
        }

        public bool IsActive => !End.HasValue;

        // Friendly display used by list UI (e.g. "13" or "110-114" or "13 → 20" when active and previewing)
        public string Display
        {
            get
            {
                if (End.HasValue)
                    return (Start == End.Value) ? Start.ToString() : $"{Start}-{End.Value}";
                // Active segment: show a preview end if provided (e.g. "13 → 20"), otherwise simple arrow
                if (CurrentPreviewEnd.HasValue)
                    return $"{Start} → {CurrentPreviewEnd.Value}";
                return $"{Start} →";
            }
        }

        // UI-only highlight flag controlled by the editor (not persisted)
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHighlighted)));
                }
            }
        }

        // When a closed segment is temporarily opened (End cleared) we store the original End
        public int? OriginalEnd
        {
            get => _originalEnd;
            set
            {
                if (_originalEnd != value)
                {
                    _originalEnd = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OriginalEnd)));
                }
            }
        }

        // When the segment is active, the editor can supply a temporary preview end (current page) so the UI shows "Start → current".
        public int? CurrentPreviewEnd
        {
            get => _currentPreviewEnd;
            set
            {
                if (_currentPreviewEnd != value)
                {
                    _currentPreviewEnd = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentPreviewEnd)));
                    // Changing the preview end affects the friendly Display string
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Display)));
                }
            }
        }

        // Was this segment created as part of the current active operation (new)?
        public bool WasNew
        {
            get => _wasNew;
            set
            {
                if (_wasNew != value)
                {
                    _wasNew = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WasNew)));
                }
            }
        }

        public Segment() { }
        public Segment(int start, int? end = null)
        {
            _start = start;
            _end = end;
        }
    }
}
