using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Concrete;

/// <summary>Rozet tanımı (metinler veritabanında, koşul kodda).</summary>
public class Achievement : EntityBase
{
    public AchievementCode Code { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Icon { get; set; } = null!;

    /// <summary>Rozet kazanıldığında verilen ek puan.</summary>
    public int RewardPoints { get; set; }

    public ICollection<UserAchievement> UserAchievements { get; set; } = [];
}
