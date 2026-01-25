namespace Xenolexia.Core.Models;

/// <summary>
/// Reading session
/// </summary>
public class ReadingSession
{
    public string Id { get; set; } = string.Empty;
    public string BookId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int PagesRead { get; set; }
    public int WordsRevealed { get; set; }
    public int WordsSaved { get; set; }
    public int Duration { get; set; } // in seconds
}

/// <summary>
/// Reading statistics
/// </summary>
public class ReadingStats
{
    public int TotalBooksRead { get; set; }
    public int TotalReadingTime { get; set; } // in seconds
    public int TotalWordsLearned { get; set; }
    public int CurrentStreak { get; set; } // days
    public int LongestStreak { get; set; }
    public double AverageSessionDuration { get; set; }
    public int WordsRevealedToday { get; set; }
    public int WordsSavedToday { get; set; }
}

/// <summary>
/// User preferences
/// </summary>
public class UserPreferences
{
    public Language DefaultSourceLanguage { get; set; } = Language.En;
    public Language DefaultTargetLanguage { get; set; } = Language.Es;
    public ProficiencyLevel DefaultProficiencyLevel { get; set; } = ProficiencyLevel.Beginner;
    public double DefaultWordDensity { get; set; } = 0.3;
    public ReaderSettings ReaderSettings { get; set; } = new();
    public bool HasCompletedOnboarding { get; set; } = false;
    public bool NotificationsEnabled { get; set; } = false;
    public int DailyGoal { get; set; } = 30; // minutes
}
