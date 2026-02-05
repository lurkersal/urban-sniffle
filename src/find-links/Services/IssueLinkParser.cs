namespace FindLinks.Services;

using System.Text.RegularExpressions;
using System.Collections.Generic;

public class IssueLinkParser
{
    // Identify links using volume and number as vol/vol./volume and no/no./number.
    // Matches: Vol 12 No 3, Vol. 12 No. 3, Volume 12 Number 3, etc.
    // Updated regex: allow any whitespace (including newlines) between vol/no/volume/number and numbers
    private static readonly Regex IssueLinkRegex = new(@"(vol(?:ume)?)[\s\.:/]*([0-9]+)[\s\S]*?(no(?:\.|umber)?)[\s\.:/]*([0-9]+)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Finds issue links and returns a list of (volume, number) tuples.
    /// </summary>
    public List<(int volume, int number)> FindIssueLinks(string text)
    {
        var links = new List<(int volume, int number)>();
        foreach (Match match in IssueLinkRegex.Matches(text))
        {
            if (match.Success &&
                int.TryParse(match.Groups[2].Value, out int vol) &&
                int.TryParse(match.Groups[4].Value, out int num))
            {
                links.Add((vol, num));
            }
        }
        return links;
    }
}
