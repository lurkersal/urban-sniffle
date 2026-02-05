namespace MagazineParser.Interfaces;

public interface IUserInteraction
{
    void DisplayMessage(string message);
    string? PromptUser(string prompt);
    bool ConfirmAction(string message);
    string? GetCorrectedLine(string originalLine);
    int ChooseCategoryAction(string categoryName);
    void DisplayInsertSummary(string category, string? title, string? modelName, int? age, string? measurements, string? photographer, List<int> pages, int imageCount);
    bool ConfirmInsert();
    int ChooseIssueResolution(string categoryName, int affectedCount);
}
