using System;
using System.Collections.Generic;
using System.Linq;
using Common.Shared;
namespace IndexEditor.Shared
{


    public static class IndexContentLineParser
    {
        public static Common.Shared.ArticleLine? Parse(string line)
        {
            try
            {
                // Use the centralized parser which understands escaped commas and handles legacy formats
                return IndexFileParser.ParseArticleLine(line);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("IndexContentLineParser.Parse", ex);
                return null;
            }
        }
    }
}
