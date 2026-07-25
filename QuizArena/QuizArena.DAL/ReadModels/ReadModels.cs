namespace QuizArena.DAL.ReadModels;

/// <summary>
/// Veri katmanına özel, hafif okuma modelleri (projeksiyonlar).
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden varlık değil, neden API DTO'su da değil?</b>
/// </para>
/// <list type="bullet">
///   <item>
///     Varlık döndürmek gereksiz kolon çeker ve <c>COUNT</c> gibi türetilmiş
///     değerleri taşıyamaz; sonuçta BLL her kategori için ayrı sayım sorgusu
///     atmak zorunda kalır (N+1 problemi).
///   </item>
///   <item>
///     Doğrudan API DTO'su döndürmek ise veri katmanını sunum sözleşmesine
///     bağlar: API yanıtına bir alan eklendiğinde SQL sorgusu değişmek
///     zorunda kalır.
///   </item>
/// </list>
/// <para>
/// Ara katman olarak bu okuma modelleri, tek SQL sorgusuyla ihtiyaç duyulan
/// veriyi getirir; BLL onu sunum DTO'suna çevirir.
/// </para>
/// </remarks>
public sealed record CategoryWithQuestionCount(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Icon,
    string? ColorHex,
    bool IsActive,
    int DisplayOrder,
    int ActiveQuestionCount);

/// <summary>Odadaki bir katılımcının anlık skor satırı.</summary>
public sealed record ScoreboardRow(
    Guid UserId,
    string Nickname,
    string? AvatarUrl,
    int TotalScore,
    int CorrectCount,
    bool IsFinished);

/// <summary>Sıralama tablosu satırı (istatistik + kullanıcı birleşimi).</summary>
public sealed record LeaderboardRow(
    Guid UserId,
    string Nickname,
    string? AvatarUrl,
    int TotalScore,
    int TotalCompetitions,
    int TotalQuestionsAnswered,
    int TotalCorrectAnswers,
    int BestStreak);
