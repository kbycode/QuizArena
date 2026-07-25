using QuizArena.Core.Entities;
using QuizArena.Entities.Dtos.Statistics;

namespace QuizArena.Entities.Dtos.Play;

/// <summary>Yarışma bitiş ekranı.</summary>
/// <param name="Rank">Çok oyunculu odada sıralama; tek kişilik yarışmada <c>null</c>.</param>
public sealed record CompetitionSummaryResponse(
    Guid CompetitionId,
    Guid RoomId,
    string CategoryName,
    int TotalScore,
    int QuestionCount,
    int CorrectCount,
    int WrongCount,
    int TimedOutCount,
    double AccuracyPercentage,
    int LongestStreak,
    int DurationSeconds,
    int? Rank,
    IReadOnlyList<ScoreboardEntryResponse> Scoreboard,
    IReadOnlyList<AchievementResponse> NewAchievements) : IDto;

/// <summary>Skor tablosu satırı.</summary>
public sealed record ScoreboardEntryResponse(
    int Rank,
    Guid UserId,
    string Nickname,
    string? AvatarUrl,
    int TotalScore,
    int CorrectCount,
    bool IsFinished) : IDto;
