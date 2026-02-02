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

/// <summary>
/// A segment of reader content for display: plain text or a foreign word with popup/save.
/// SaveWordCommand is set by the view layer (ReaderViewModel) so the tooltip Save button can invoke it.
/// </summary>
public class ReaderContentSegment
{
    public string Text { get; set; } = string.Empty;
    public bool IsForeign { get; set; }
    public ForeignWordData? WordData { get; set; }
    /// <summary>Command to save this word to vocabulary (set by ViewModel; type ICommand at runtime).</summary>
    public object? SaveWordCommand { get; set; }
    /// <summary>Command to notify that this word was revealed (tooltip opened); set by ViewModel.</summary>
    public object? NotifyRevealedCommand { get; set; }
}
