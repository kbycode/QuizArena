using QuizArena.Core.Entities;
using QuizArena.Core.Entities.Concrete;

namespace QuizArena.Entities.Concrete;

/// <summary>
/// Kullanıcının kazandığı rozet. <c>(UserId, AchievementId)</c> tekildir:
/// aynı rozet iki kez verilemez.
/// </summary>
public class UserAchievement : EntityBase
{
    public Guid UserId { get; set; }
    public Guid AchievementId { get; set; }

    public DateTime EarnedAtUtc { get; set; }

    /// <summary>Rozetin kazanıldığı yarışma (varsa). İzlenebilirlik için.</summary>
    public Guid? CompetitionId { get; set; }

    public User User { get; set; } = null!;
    public Achievement Achievement { get; set; } = null!;
}
