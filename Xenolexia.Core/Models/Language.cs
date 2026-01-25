namespace Xenolexia.Core.Models;

/// <summary>
/// Supported language codes
/// </summary>
public enum Language
{
    En, // English
    El, // Greek
    Es, // Spanish
    Fr, // French
    De, // German
    It, // Italian
    Pt, // Portuguese
    Ru, // Russian
    Ja, // Japanese
    Zh, // Chinese
    Ko, // Korean
    Ar, // Arabic
    Nl, // Dutch
    Pl, // Polish
    Tr, // Turkish
    Sv, // Swedish
    Da, // Danish
    Fi, // Finnish
    No, // Norwegian
    Cs, // Czech
    Hu, // Hungarian
    Ro, // Romanian
    Uk, // Ukrainian
    He, // Hebrew
    Hi, // Hindi
    Th, // Thai
    Vi, // Vietnamese
    Id  // Indonesian
}

/// <summary>
/// Language metadata for display purposes
/// </summary>
public class LanguageInfo
{
    public Language Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string? Flag { get; set; } // Emoji flag
    public bool Rtl { get; set; } // Right-to-left language
}

/// <summary>
/// Language pair for translation
/// </summary>
public class LanguagePair
{
    public Language SourceLanguage { get; set; }
    public Language TargetLanguage { get; set; }
}

/// <summary>
/// Proficiency levels
/// </summary>
public enum ProficiencyLevel
{
    Beginner,
    Intermediate,
    Advanced
}

/// <summary>
/// CEFR levels
/// </summary>
public enum CEFRLevel
{
    A1, A2, B1, B2, C1, C2
}
