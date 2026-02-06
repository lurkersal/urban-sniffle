namespace Common.Shared
{
    public class Segment
    {
        public int Start { get; set; }
        public int? End { get; set; }
        public bool IsActive => !End.HasValue;

        public Segment() { }
        public Segment(int start, int? end = null)
        {
            Start = start;
            End = end;
        }
    }
}
