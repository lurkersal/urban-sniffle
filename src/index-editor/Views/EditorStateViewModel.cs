using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using Common.Shared;
using IndexEditor.Shared;
namespace IndexEditor.Views
{
    public class EditorStateViewModel
    {
        public SelectArticleCommand SelectArticleCommand { get; }
        public ArticleLine? SelectedArticle { get; set; }
        public ObservableCollection<Common.Shared.ArticleLine> Articles { get; } = new();

        // Removed per-card helper properties. Bind directly to ArticleLine properties in the UI and use value converters or formatting in the DataTemplate.

        public EditorStateViewModel()
        {
            SelectArticleCommand = new SelectArticleCommand(this);
            // Initialize from static EditorState
            foreach (var article in EditorState.Articles)
                Articles.Add(article);
            // Debug: Print type of every item
            for (int i = 0; i < Articles.Count; i++)
            {
                var a = Articles[i];
                if (a != null)
                    System.Console.WriteLine($"[DEBUG] Articles[{i}] type: {a.GetType().FullName}");
            }
            // Listen for changes if needed
            EditorState.StateChanged += SyncArticles;

            // Debug: Print formatted text for each article
            for (int i = 0; i < Articles.Count; i++)
            {
                var a = Articles[i];
                if (a != null)
                {
                    string ageText = string.Join(", ", a.Ages.Where(age => age.HasValue).Select(age => age.Value.ToString()));
                    string modelText = string.Join(", ", a.ModelNames);
                    string photographerText = string.Join(", ", a.Photographers);
                    string measurementsText = string.Join(", ", a.Measurements);
                    System.Console.WriteLine($"[DEBUG] Card[{i}] Formatted: Model='{modelText}', Age='{ageText}', Photographer='{photographerText}', Measurements='{measurementsText}'");
                }
            }
        }

        private void SyncArticles()
        {
            Articles.Clear();
            int i = 0;
            foreach (var article in EditorState.Articles)
            {
                Articles.Add(article);
                System.Console.WriteLine($"[DEBUG] Card[{i}] Category='{article.Category}', Title='{article.Title}', Pages=[{string.Join(",", article.Pages)}]");
                if (article != null)
                {
                    string ageText = string.Join(", ", article.Ages.Where(age => age.HasValue).Select(age => age.Value.ToString()));
                    string modelText = string.Join(", ", article.ModelNames);
                    string photographerText = string.Join(", ", article.Photographers);
                    string measurementsText = string.Join(", ", article.Measurements);
                    System.Console.WriteLine($"[DEBUG] Card[{i}] Formatted: Model='{modelText}', Age='{ageText}', Photographer='{photographerText}', Measurements='{measurementsText}'");
                }
                i++;
            }
            System.Console.WriteLine($"[DEBUG] Total cards in list: {Articles.Count}");
        }
    }
}

