
using common.Shared.Models;

namespace common.Shared.Interfaces
{
    public interface IContentParser
    {
        ContentLine? ParseContentLine(string line);
        bool IsHeaderLine(string line, out string title, out int volume, out int number);
    }
}