namespace Xenolexia.Core.Models;

/// <summary>
/// Reader theme
/// </summary>
public enum ReaderTheme
{
    Light,
    Dark,
    Sepia
}

/// <summary>
/// Reader settings
/// </summary>
public class ReaderSettings
{
    public ReaderTheme Theme { get; set; } = ReaderTheme.Light;
    public string FontFamily { get; set; } = "System";
    public double FontSize { get; set; } = 16; // in sp/pt
    public double LineHeight { get; set; } = 1.6; // multiplier
    public double MarginHorizontal { get; set; } = 24; // in dp/pt
    public double MarginVertical { get; set; } = 16; // in dp/pt
    public TextAlign TextAlign { get; set; } = TextAlign.Left;
    public double Brightness { get; set; } = 1.0; // 0.0 - 1.0
}

/// <summary>
/// Text alignment
/// </summary>
public enum TextAlign
{
    Left,
    Justify
}

/// <summary>
/// Foreign word data in processed content
/// </summary>
public class ForeignWordData
{
    public string OriginalWord { get; set; } = string.Empty;
    public string ForeignWord { get; set; } = string.Empty;
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public WordEntry WordEntry { get; set; } = new();
}

/// <summary>
/// Processed chapter with foreign words
/// </summary>
public class ProcessedChapter : Chapter
{
    public List<ForeignWordData> ForeignWords { get; set; } = new();
    public string ProcessedContent { get; set; } = string.Empty; // HTML with foreign words marked
}
