using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Dtos.Statistics;

/// <summary>Rozet kartı.</summary>
/// <param name="EarnedAtUtc">
/// Kazanılma zamanı. <c>null</c> ise rozet henüz kazanılmamıştır — aynı DTO
/// hem "rozetlerim" hem "kazanılabilecek rozetler" listesinde kullanılabilir.
/// </param>
public sealed record AchievementResponse(
    AchievementCode Code,
    string Name,
    string Description,
    string Icon,
    int RewardPoints,
    DateTime? EarnedAtUtc) : IDto;
