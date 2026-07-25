using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Statistics;

/// <summary>Kullanıcının istatistik kartı.</summary>
public sealed record UserStatisticResponse(
    Guid UserId,
    string Nickname,
    string? AvatarUrl,
    int TotalCompetitions,
    int TotalQuestionsAnswered,
    int TotalCorrectAnswers,
    int TotalScore,
    double AccuracyPercentage,
    int BestStreak,
    int BestCompetitionScore,
    int WinCount,
    int AverageAnswerMilliseconds,
    DateTime? LastPlayedAtUtc,
    IReadOnlyList<AchievementResponse> Achievements) : IDto;
