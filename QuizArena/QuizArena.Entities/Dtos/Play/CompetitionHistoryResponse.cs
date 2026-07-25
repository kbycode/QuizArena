using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Play;

/// <summary>Yarışma geçmişi listesi satırı.</summary>
public sealed record CompetitionHistoryResponse(
    Guid CompetitionId,
    string CategoryName,
    string? CategoryIcon,
    int TotalScore,
    int QuestionCount,
    int CorrectCount,
    double AccuracyPercentage,
    int LongestStreak,
    int DurationSeconds,
    DateTime FinishedAtUtc) : IDto;
