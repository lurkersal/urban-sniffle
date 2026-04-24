using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using IndexEditor.Views;
using System;
using System.Collections.Generic;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service responsible for rendering article cards for display in the page controller.
    /// Handles the visual representation and layout of article information.
    /// </summary>
    public class ArticleCardRenderer : IArticleCardRenderer
    {
        private IndexEditor.Shared.IEditorState? _editorState;

        public void SetEditorState(IndexEditor.Shared.IEditorState editorState)
        {
            _editorState = editorState;
        }

        /// <summary>
        /// Creates a visual card for an article with colored bar, title, details, and category.
        /// </summary>
        /// <param name="article">The article to render</param>
        /// <returns>A Border control containing the rendered article card, or null if rendering fails</returns>
        public Border? CreateArticleCard(Common.Shared.ArticleLine article)
        {
            if (article == null)
            {
                return null;
            }

            try
            {
                // Create the card border
                var cardBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                // Create grid with 4 columns: color bar | content | checkbox | category
                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(GridLength.Auto),    // Color bar
                        new ColumnDefinition(GridLength.Star),    // Content
                        new ColumnDefinition(GridLength.Auto),    // Checkbox
                        new ColumnDefinition(GridLength.Auto)     // Category label
                    }
                };

                // Add color bar
                var colorBar = CreateColorBar(article.Category);
                Grid.SetColumn(colorBar, 0);
                grid.Children.Add(colorBar);

                // Add content stack
                var contentStack = CreateContentStack(article);
                Grid.SetColumn(contentStack, 1);
                grid.Children.Add(contentStack);

                // Add thumbnail checkbox
                var thumbnailCheckbox = CreateThumbnailCheckbox(article);
                Grid.SetColumn(thumbnailCheckbox, 2);
                grid.Children.Add(thumbnailCheckbox);

                // Add category label
                var categoryText = CreateCategoryLabel(article.Category);
                Grid.SetColumn(categoryText, 3);
                grid.Children.Add(categoryText);

                cardBorder.Child = grid;
                return cardBorder;
            }
            catch (Exception ex)
            {
                Shared.DebugLogger.LogException("ArticleCardRenderer.CreateArticleCard", ex);
                return null;
            }
        }

        /// <summary>
        /// Creates the colored bar for the article card based on category
        /// </summary>
        private Border CreateColorBar(string? category)
        {
            var colorBarOuter = new Border
            {
                Width = 16,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 8, 0)
            };

            // Get category color
            var converter = new ArticleCategoryToColorConverter();
            var colorBrush = converter.Convert(category, typeof(SolidColorBrush), null,
                System.Globalization.CultureInfo.InvariantCulture) as SolidColorBrush;

            var colorBarInner = new Border
            {
                Background = colorBrush ?? new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                Opacity = 0.6,
                CornerRadius = new CornerRadius(1)
            };
            
            colorBarOuter.Child = colorBarInner;
            return colorBarOuter;
        }

        /// <summary>
        /// Creates the content stack containing title and details
        /// </summary>
        private StackPanel CreateContentStack(Common.Shared.ArticleLine article)
        {
            var contentStack = new StackPanel
            {
                Spacing = 2
            };

            // Add title
            var titleText = new TextBlock
            {
                Text = !string.IsNullOrWhiteSpace(article.Title) ? article.Title : article.Category,
                FontWeight = FontWeight.Bold,
                FontSize = 16
            };
            contentStack.Children.Add(titleText);

            // Add details if available
            var detailsText = CreateDetailsText(article);
            if (detailsText != null)
            {
                contentStack.Children.Add(detailsText);
            }

            return contentStack;
        }

        /// <summary>
        /// Creates the details text block showing model name, age, and contributor based on category
        /// </summary>
        private TextBlock? CreateDetailsText(Common.Shared.ArticleLine article)
        {
            var details = GetArticleDetails(article);
            
            if (details.Count == 0)
            {
                return null;
            }

            return new TextBlock
            {
                Text = string.Join(" • ", details),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66))
            };
        }

        /// <summary>
        /// Determines which details to show based on article category
        /// </summary>
        private List<string> GetArticleDetails(Common.Shared.ArticleLine article)
        {
            var details = new List<string>();
            var category = (article.Category ?? string.Empty).ToLowerInvariant();

            // Show model name and age for specific categories
            if (ShouldShowModelDetails(category))
            {
                if (!string.IsNullOrWhiteSpace(article.ModelName0))
                {
                    details.Add(article.ModelName0);
                }
                if (!string.IsNullOrWhiteSpace(article.Age0))
                {
                    details.Add(article.Age0);
                }
            }

            // Show contributor for all categories (as photographer, author, illustrator, etc.)
            if (!string.IsNullOrWhiteSpace(article.Contributor0))
            {
                details.Add(article.Contributor0);
            }

            return details;
        }

        /// <summary>
        /// Determines if a category should show model details (name and age)
        /// </summary>
        private bool ShouldShowModelDetails(string category)
        {
            return category == "model" 
                || category == "cover" 
                || category == "group" 
                || category == "wives" 
                || category == "interview";
        }

        /// <summary>
        /// Creates the thumbnail checkbox for the article card.
        /// Checkbox indicates whether the current page is the thumbnail page for this article.
        /// </summary>
        private CheckBox CreateThumbnailCheckbox(Common.Shared.ArticleLine article)
        {
            var currentPage = _editorState?.CurrentPage ?? 0;
            
            // Check if current page is the thumbnail page (or first page if ThumbnailPage is null)
            bool isCurrentPageThumbnail = false;
            if (article.ThumbnailPage.HasValue)
            {
                isCurrentPageThumbnail = (article.ThumbnailPage.Value == currentPage);
            }
            else if (article.Pages != null && article.Pages.Count > 0)
            {
                // Default: first page is the thumbnail
                isCurrentPageThumbnail = (article.Pages[0] == currentPage);
            }

            var checkbox = new CheckBox
            {
                Content = "Thumbnail",
                IsChecked = isCurrentPageThumbnail,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0),
                Tag = article // Store article reference for event handler
            };

            checkbox.Click += OnThumbnailCheckboxClicked;

            return checkbox;
        }

        /// <summary>
        /// Handles thumbnail checkbox clicks. Updates the article's ThumbnailPage property.
        /// </summary>
        private void OnThumbnailCheckboxClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                if (sender is not CheckBox checkbox) return;
                if (checkbox.Tag is not Common.Shared.ArticleLine article) return;
                if (_editorState == null) return;

                var currentPage = _editorState.CurrentPage;

                // If checkbox is checked, set current page as thumbnail
                if (checkbox.IsChecked == true)
                {
                    // If this is the first page, set to null (default behavior)
                    if (article.Pages != null && article.Pages.Count > 0 && article.Pages[0] == currentPage)
                    {
                        article.ThumbnailPage = null;
                    }
                    else
                    {
                        article.ThumbnailPage = currentPage;
                    }
                    
                    Shared.DebugLogger.Log($"Thumbnail page set to: {article.ThumbnailPage?.ToString() ?? "(default/first page)"}");
                }
                else
                {
                    // If unchecked, reset to default (first page)
                    article.ThumbnailPage = null;
                    Shared.DebugLogger.Log("Thumbnail page reset to default (first page)");
                }

                // Notify state change to refresh UI
                _editorState.NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Shared.DebugLogger.LogException("ArticleCardRenderer.OnThumbnailCheckboxClicked", ex);
            }
        }

        /// <summary>
        /// Creates the category label text block
        /// </summary>
        private TextBlock CreateCategoryLabel(string? category)
        {
            return new TextBlock
            {
                Text = category ?? "Unknown",
                FontWeight = FontWeight.Bold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(8, 0, 0, 0)
            };
        }
    }
}

