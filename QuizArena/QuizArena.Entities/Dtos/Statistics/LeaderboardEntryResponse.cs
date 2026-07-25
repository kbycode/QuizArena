using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Statistics;

/// <summary>Genel sıralama tablosu satırı.</summary>
public sealed record LeaderboardEntryResponse(
    int Rank,
    Guid UserId,
    string Nickname,
    string? AvatarUrl,
    int TotalScore,
    int TotalCompetitions,
    double AccuracyPercentage,
    int BestStreak) : IDto;
