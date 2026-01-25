using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Translation service using LibreTranslate API
/// </summary>
public class TranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public TranslationService(string? apiUrl = null)
    {
        _httpClient = new HttpClient();
        _apiUrl = apiUrl ?? "https://libretranslate.com";
    }

    public async Task<string> TranslateAsync(string text, Language sourceLanguage, Language targetLanguage)
    {
        try
        {
            var sourceCode = GetLanguageCode(sourceLanguage);
            var targetCode = GetLanguageCode(targetLanguage);

            var requestBody = new
            {
                q = text,
                source = sourceCode,
                target = targetCode,
                format = "text"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiUrl}/translate", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
            return result?.TranslatedText ?? text;
        }
        catch
        {
            // Fallback: return original text if translation fails
            return text;
        }
    }

    public async Task<Dictionary<string, string>> TranslateBatchAsync(List<string> words, Language sourceLanguage, Language targetLanguage)
    {
        var results = new Dictionary<string, string>();
        
        // Translate in batches to avoid rate limits
        const int batchSize = 10;
        for (int i = 0; i < words.Count; i += batchSize)
        {
            var batch = words.Skip(i).Take(batchSize).ToList();
            var batchText = string.Join(" ", batch);
            var translated = await TranslateAsync(batchText, sourceLanguage, targetLanguage);
            var translatedWords = translated.Split(' ');

            for (int j = 0; j < batch.Count && j < translatedWords.Length; j++)
            {
                results[batch[j]] = translatedWords[j];
            }
        }

        return results;
    }

    private string GetLanguageCode(Language language)
    {
        return language switch
        {
            Language.En => "en",
            Language.Es => "es",
            Language.Fr => "fr",
            Language.De => "de",
            Language.It => "it",
            Language.Pt => "pt",
            Language.Ru => "ru",
            Language.Ja => "ja",
            Language.Zh => "zh",
            Language.Ko => "ko",
            Language.Ar => "ar",
            Language.El => "el",
            Language.Nl => "nl",
            Language.Pl => "pl",
            Language.Tr => "tr",
            Language.Sv => "sv",
            Language.Da => "da",
            Language.Fi => "fi",
            Language.No => "no",
            Language.Cs => "cs",
            Language.Hu => "hu",
            Language.Ro => "ro",
            Language.Uk => "uk",
            Language.He => "he",
            Language.Hi => "hi",
            Language.Th => "th",
            Language.Vi => "vi",
            Language.Id => "id",
            _ => "en"
        };
    }

    private class TranslationResponse
    {
        public string TranslatedText { get; set; } = string.Empty;
    }
}
