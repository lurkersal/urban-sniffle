namespace Common.Shared
{
    using System.ComponentModel;

    public class Segment : INotifyPropertyChanged
    {
        private int _start;
        private int? _end;

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

        // Friendly display used by list UI (e.g. "13" or "110-114" or "13 →" when active)
        public string Display => End.HasValue ? (Start == End.Value ? Start.ToString() : $"{Start}-{End.Value}") : $"{Start} →";

        public Segment() { }
        public Segment(int start, int? end = null)
        {
            _start = start;
            _end = end;
        }
    }
}
