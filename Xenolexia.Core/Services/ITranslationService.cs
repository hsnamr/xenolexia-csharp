using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Translation service interface
/// </summary>
public interface ITranslationService
{
    Task<string> TranslateAsync(string text, Language sourceLanguage, Language targetLanguage);
    Task<Dictionary<string, string>> TranslateBatchAsync(List<string> words, Language sourceLanguage, Language targetLanguage);
}
