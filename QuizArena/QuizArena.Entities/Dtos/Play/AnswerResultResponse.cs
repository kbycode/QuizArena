using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Play;

/// <summary>
/// Cevabın sonucu. Doğru cevap <b>ancak bu noktada</b> açıklanır — oyuncu
/// tercihini gönderdikten sonra.
/// </summary>
/// <param name="CompetitionId">
/// Yarışma kimliği. Son soru cevaplandığında istemci bu değerle sonuç
/// ekranını (<c>GET /api/play/summary/{competitionId}</c>) çağırır; ayrıca
/// arama yapmak zorunda kalmaz.
/// </param>
/// <param name="PointsBreakdown">
/// Puanın nasıl oluştuğu. Oyuncunun "neden 340 puan aldım?" sorusunun cevabı
/// arayüzde gösterilebilir; şeffaflık, oyunun adil algılanması için önemlidir.
/// </param>
public sealed record AnswerResultResponse(
    Guid CompetitionId,
    bool IsCorrect,
    bool IsTimedOut,
    Guid CorrectAnswerId,
    string CorrectAnswerText,
    string? Explanation,
    int ElapsedMilliseconds,
    ScoreBreakdown PointsBreakdown,
    int TotalScore,
    int CurrentStreak,
    int AnsweredCount,
    int TotalQuestions,
    bool HasNextQuestion) : IDto;

/// <summary>Puan kalemleri.</summary>
public sealed record ScoreBreakdown(
    int BasePoints,
    int SpeedBonus,
    int StreakBonus,
    int Total) : IDto;
