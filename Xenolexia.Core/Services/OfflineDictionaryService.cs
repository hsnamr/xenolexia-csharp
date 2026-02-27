using System.Collections.Concurrent;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Offline dictionary service. Dictionaries are stored in app data and auto-installed
/// when the user selects a target language. Uses bundled JSON first, then tries download.
/// </summary>
public class OfflineDictionaryService : IOfflineDictionaryService
{
    private readonly string _dictionariesDir;
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _loaded = new();
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>Base URL for downloading dictionaries when not bundled. Set to null to disable download.</summary>
    public static string? DictionaryDownloadBaseUrl { get; set; } =
        "https://raw.githubusercontent.com/xenolexia/xenolexia-dictionaries/main/";

    public OfflineDictionaryService(string? appDataPath = null)
    {
        _dictionariesDir = appDataPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".xenolexia", "dictionaries");
        Directory.CreateDirectory(_dictionariesDir);
    }

    public async Task EnsureDictionaryLoadedAsync(Language sourceLanguage, Language targetLanguage)
    {
        var key = DictKey(sourceLanguage, targetLanguage);
        if (_loaded.ContainsKey(key))
            return;

        var fileName = $"{sourceLanguage.ToString().ToLowerInvariant()}-{targetLanguage.ToString().ToLowerInvariant()}.json";
        var filePath = Path.Combine(_dictionariesDir, fileName);

        if (!File.Exists(filePath))
        {
            await CopyBundledDictionaryAsync(sourceLanguage, targetLanguage, filePath);
            if (!File.Exists(filePath))
                await TryDownloadDictionaryAsync(sourceLanguage, targetLanguage, filePath);
        }

        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            _loaded[key] = raw != null
                ? new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            // Embedded copy failed; try loading directly from assembly, then from memory if we downloaded
            _loaded[key] = await LoadFromEmbeddedAsync(sourceLanguage, targetLanguage)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task CopyBundledDictionaryAsync(Language source, Language target, string destPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = GetResourceName(source, target);

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? FindResourceStream(assembly, source, target);
        if (stream != null)
        {
            await using var fs = File.Create(destPath);
            await stream.CopyToAsync(fs);
        }
    }

    private static async Task<Dictionary<string, string>?> LoadFromEmbeddedAsync(Language source, Language target)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(GetResourceName(source, target))
            ?? FindResourceStream(assembly, source, target);
        if (stream == null)
            return null;
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return raw != null ? new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase) : null;
    }

    private static string GetResourceName(Language source, Language target) =>
        $"Xenolexia.Core.Data.Dictionaries.{source.ToString().ToLowerInvariant()}-{target.ToString().ToLowerInvariant()}.json";

    private static Stream? FindResourceStream(Assembly assembly, Language source, Language target)
    {
        var suffix = $"{source.ToString().ToLowerInvariant()}-{target.ToString().ToLowerInvariant()}.json";
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return assembly.GetManifestResourceStream(name);
        }
        return null;
    }

    private static async Task TryDownloadDictionaryAsync(Language source, Language target, string destPath)
    {
        var url = DictionaryDownloadBaseUrl;
        if (string.IsNullOrEmpty(url)) return;

        var fileName = $"{source.ToString().ToLowerInvariant()}-{target.ToString().ToLowerInvariant()}.json";
        var fullUrl = url.TrimEnd('/') + "/" + fileName;
        try
        {
            var response = await HttpClient.GetAsync(fullUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                await File.WriteAllTextAsync(destPath, json);
            }
        }
        catch
        {
            // Download failed; user will need bundled dict or manual install
        }
    }

    public WordEntry? Lookup(string word, Language sourceLanguage, Language targetLanguage)
    {
        var key = DictKey(sourceLanguage, targetLanguage);
        if (!_loaded.TryGetValue(key, out var dict))
            return null;

        var lower = word.ToLowerInvariant();
        if (!dict.TryGetValue(lower, out var translation))
            return null;

        return new WordEntry
        {
            Id = $"{sourceLanguage}_{targetLanguage}_{lower}",
            SourceWord = lower,
            TargetWord = translation,
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage,
            ProficiencyLevel = ProficiencyLevel.Beginner,
            FrequencyRank = 0,
            PartOfSpeech = PartOfSpeech.Other
        };
    }

    public Dictionary<string, WordEntry?> LookupBatch(IEnumerable<string> words, Language sourceLanguage, Language targetLanguage)
    {
        var result = new Dictionary<string, WordEntry?>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in words.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            result[word] = Lookup(word, sourceLanguage, targetLanguage);
        }
        return result;
    }

    public bool IsDictionaryAvailable(Language sourceLanguage, Language targetLanguage)
    {
        var key = DictKey(sourceLanguage, targetLanguage);
        return _loaded.ContainsKey(key) && _loaded[key].Count > 0;
    }

    private static string DictKey(Language source, Language target) =>
        $"{source}_{target}";
}
