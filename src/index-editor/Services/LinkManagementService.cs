using System;
using System.Collections.Generic;
using System.Linq;
using Common.Shared;
using IndexEditor.Shared;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service responsible for managing magazine links (cross-references to other issues).
    /// Extracted from PageControllerView to improve separation of concerns.
    /// </summary>
    public interface ILinkManagementService
    {
        /// <summary>
        /// Gets links for a specific page.
        /// </summary>
        IReadOnlyList<MagazineLink> GetLinksForPage(int pageNumber);

        /// <summary>
        /// Sets links for a specific page.
        /// </summary>
        void SetLinksForPage(int pageNumber, IEnumerable<MagazineLink> links);

        /// <summary>
        /// Adds links from the index file to the service.
        /// </summary>
        void LoadLinksFromIndex(IEnumerable<MagazineLink> links);

        /// <summary>
        /// Gets all links across all pages.
        /// </summary>
        IReadOnlyDictionary<int, List<MagazineLink>> GetAllLinks();

        /// <summary>
        /// Clears all links.
        /// </summary>
        void ClearAllLinks();

        /// <summary>
        /// Gets the total count of links stored.
        /// </summary>
        int GetTotalLinkCount();
    }

    public class LinkManagementService : ILinkManagementService
    {
        private readonly Dictionary<int, List<MagazineLink>> _pageLinks = new();

        public IReadOnlyList<MagazineLink> GetLinksForPage(int pageNumber)
        {
            if (_pageLinks.TryGetValue(pageNumber, out var links))
            {
                return links.AsReadOnly();
            }
            return Array.Empty<MagazineLink>();
        }

        public void SetLinksForPage(int pageNumber, IEnumerable<MagazineLink> links)
        {
            if (!_pageLinks.ContainsKey(pageNumber))
            {
                _pageLinks[pageNumber] = new List<MagazineLink>();
            }
            else
            {
                _pageLinks[pageNumber].Clear();
            }

            _pageLinks[pageNumber].AddRange(links);
        }

        public void LoadLinksFromIndex(IEnumerable<MagazineLink> links)
        {
            if (links == null) return;

            foreach (var link in links)
            {
                var pageNum = link.Page;

                if (!_pageLinks.ContainsKey(pageNum))
                {
                    _pageLinks[pageNum] = new List<MagazineLink>();
                }

                // Avoid duplicates
                if (!_pageLinks[pageNum].Any(l => 
                    l.Magazine == link.Magazine && 
                    l.Volume == link.Volume && 
                    l.Issue == link.Issue))
                {
                    _pageLinks[pageNum].Add(link);
                }
            }

            DebugLogger.Log($"LinkManagementService: Loaded {GetTotalLinkCount()} unique links from index");
        }

        public IReadOnlyDictionary<int, List<MagazineLink>> GetAllLinks()
        {
            return _pageLinks;
        }

        public void ClearAllLinks()
        {
            _pageLinks.Clear();
        }

        public int GetTotalLinkCount()
        {
            return _pageLinks.Values.Sum(list => list.Count);
        }
    }
}

