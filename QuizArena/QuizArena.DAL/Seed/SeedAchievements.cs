using QuizArena.Entities.Concrete;
using QuizArena.Entities.Enums;

namespace QuizArena.DAL.Seed;

/// <summary>
/// Rozet tanımları.
/// </summary>
/// <remarks>
/// <para>
/// <c>internal</c> değil <c>public</c>: rozet kataloğu uygulamanın çalışması
/// için gereken referans veridir ve testler de aynı katalogla çalışmak zorunda
/// (aksi hâlde test kendi rozetlerini uydurur ve gerçek kodla ayrışır).
/// </para>
/// <para>
/// Ad ve açıklama seed dilinde yazılır. Rozetler veritabanında tek kayıt
/// olarak duruyor; her istekte çevrilmiyorlar çünkü <b>içerik</b>ler, arayüz
/// metni değil — tıpkı kategori ve soru metinleri gibi.
/// </para>
/// </remarks>
public static class SeedAchievements
{
    /// <param name="english">
    /// <c>true</c> ise İngilizce katalog. Varsayılan Türkçe: testler ve
    /// mevcut çağrı yerleri parametresiz çalışmaya devam eder.
    /// </param>
    public static Achievement[] Create(bool english = false) => english
        ?
        [
            Make(AchievementCode.FirstBlood, "First Step", "You finished your first game.", "🎯", 50),
            Make(AchievementCode.Perfectionist, "Flawless", "You answered every question correctly in one game.", "💎", 500),
            Make(AchievementCode.QuickThinker, "Lightning", "You answered a question correctly in under 3 seconds.", "⚡", 150),
            Make(AchievementCode.Veteran, "Veteran", "You completed 10 games.", "🎖️", 300),
            Make(AchievementCode.StreakMaster, "Streak Master", "You answered 10 questions correctly in a row.", "🔥", 400),
            Make(AchievementCode.Champion, "Champion", "You finished first in a multiplayer game.", "🏆", 600)
        ]
        :
        [
            Make(AchievementCode.FirstBlood, "İlk Adım", "İlk yarışmanı tamamladın.", "🎯", 50),
            Make(AchievementCode.Perfectionist, "Kusursuz", "Bir yarışmada tüm soruları doğru cevapladın.", "💎", 500),
            Make(AchievementCode.QuickThinker, "Şimşek", "Bir soruyu 3 saniyenin altında doğru cevapladın.", "⚡", 150),
            Make(AchievementCode.Veteran, "Kıdemli", "10 yarışma tamamladın.", "🎖️", 300),
            Make(AchievementCode.StreakMaster, "Seri Katil", "Üst üste 10 soruyu doğru cevapladın.", "🔥", 400),
            Make(AchievementCode.Champion, "Şampiyon", "Çok oyunculu bir yarışmayı birinci bitirdin.", "🏆", 600)
        ];

    private static Achievement Make(
        AchievementCode code,
        string name,
        string description,
        string icon,
        int rewardPoints) => new()
        {
            Code = code,
            Name = name,
            Description = description,
            Icon = icon,
            RewardPoints = rewardPoints
        };
}
