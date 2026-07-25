using QuizArena.Entities.Concrete;
using QuizArena.Entities.Enums;

namespace QuizArena.DAL.Seed;

/// <summary>
/// Rozet tanımları.
/// </summary>
/// <remarks>
/// <c>internal</c> değil <c>public</c>: rozet kataloğu uygulamanın çalışması
/// için gereken referans veridir ve testler de aynı katalogla çalışmak zorunda
/// (aksi hâlde test kendi rozetlerini uydurur ve gerçek kodla ayrışır).
/// </remarks>
public static class SeedAchievements
{
    public static Achievement[] Create() =>
    [
        new()
        {
            Code = AchievementCode.FirstBlood,
            Name = "İlk Adım",
            Description = "İlk yarışmanı tamamladın.",
            Icon = "🎯",
            RewardPoints = 50
        },
        new()
        {
            Code = AchievementCode.Perfectionist,
            Name = "Kusursuz",
            Description = "Bir yarışmada tüm soruları doğru cevapladın.",
            Icon = "💎",
            RewardPoints = 500
        },
        new()
        {
            Code = AchievementCode.QuickThinker,
            Name = "Şimşek",
            Description = "Bir soruyu 3 saniyenin altında doğru cevapladın.",
            Icon = "⚡",
            RewardPoints = 150
        },
        new()
        {
            Code = AchievementCode.Veteran,
            Name = "Kıdemli",
            Description = "10 yarışma tamamladın.",
            Icon = "🎖️",
            RewardPoints = 300
        },
        new()
        {
            Code = AchievementCode.StreakMaster,
            Name = "Seri Katil",
            Description = "Üst üste 10 soruyu doğru cevapladın.",
            Icon = "🔥",
            RewardPoints = 400
        },
        new()
        {
            Code = AchievementCode.Champion,
            Name = "Şampiyon",
            Description = "Çok oyunculu bir yarışmayı birinci bitirdin.",
            Icon = "🏆",
            RewardPoints = 600
        }
    ];
}
