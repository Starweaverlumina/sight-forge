using System.Net.Http.Headers;
using System.Windows;
using SightForge.Models;
using SightForge.Services;

namespace SightForge;

public partial class GuideForgeWindow : Window
{
    private readonly AccessibleNarrationService _narrator;

    public GuideForgeWindow(AccessibleNarrationService narrator)
    {
        InitializeComponent();
        _narrator = narrator;
    }

    private async void ResearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GameText.Text) || string.IsNullOrWhiteSpace(ApiKeyBox.Password))
        {
            StatusText.Text = "Enter the game title and a session API key.";
            return;
        }

        try
        {
            StatusText.Text = "Researching approved sources...";
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.openai.com/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKeyBox.Password.Trim());

            var provider = new OpenAiWebResearchProvider(httpClient);
            var service = new GameResearchService(provider);
            var question = QuestionText.Text.Trim();
            if (SpoilerCheck.IsChecked != true) question += " Avoid story and puzzle spoilers unless essential to answer the question.";

            var result = await service.ResearchAsync(
                new GameResearchRequest
                {
                    GameTitle = GameText.Text.Trim(),
                    Platform = PlatformText.Text.Trim(),
                    Topic = "accessible gameplay help",
                    UserQuestion = question
                },
                new GameResearchPolicy
                {
                    WebResearchEnabled = true,
                    RequireUserConfirmationForNewDomains = false,
                    PreferOfficialSources = true
                });

            AnswerText.Text = result.Summary;
            StatusText.Text = result.FromCache ? "Answer loaded from the local cache." : "Research complete.";
            _narrator.Speak(new NarrationRequest("GuideForge answer ready.", NarrationPriority.Important));
        }
        catch (Exception exception)
        {
            StatusText.Text = "Research failed: " + exception.Message;
            _narrator.Speak(new NarrationRequest("GuideForge research failed.", NarrationPriority.Important));
        }
        finally
        {
            ApiKeyBox.Clear();
        }
    }

    private void SpeakButton_Click(object sender, RoutedEventArgs e) =>
        _narrator.Speak(new NarrationRequest(AnswerText.Text, NarrationPriority.Important));
}
