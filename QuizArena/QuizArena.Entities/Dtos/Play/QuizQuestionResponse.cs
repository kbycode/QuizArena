using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Dtos.Play;

/// <summary>
/// Oyuncuya servis edilen soru.
/// </summary>
/// <remarks>
/// <b>Bu tipin en önemli özelliği, içermediği alandır.</b> Ne bu kayıtta ne de
/// <see cref="QuizOptionResponse"/>'ta "doğru cevap" bilgisi <b>yoktur</b> —
/// dolayısıyla yanıtı kim serileştirirse serileştirsin doğru cevap istemciye
/// sızamaz. Doğru cevap, ancak oyuncu cevabını gönderdikten sonra
/// <see cref="AnswerResultResponse"/> ile açıklanır.
/// <para>
/// Ölçüt şu: <c>Answer</c> varlığını doğruluk alanıyla birlikte döndüren
/// <b>tek bir uç</b>, yarışmayı tarayıcı konsolundan kazanılabilir hâle
/// getirir. Bu yüzden doğruluk bilgisi oyuncuya giden hiçbir tipte yer almaz.
/// </para>
/// </remarks>
public sealed record QuizQuestionResponse(
    Guid CompetitionQuestionId,
    int Order,
    int TotalQuestions,
    string Text,
    string CategoryName,
    QuestionDifficulty Difficulty,
    int TimeLimitSeconds,
    DateTime ClosesAtUtc,
    int CurrentScore,
    int CurrentStreak,
    IReadOnlyList<QuizOptionResponse> Options) : IDto;

/// <summary>Şık. Yalnızca kimlik ve metin — doğruluk bilgisi taşımaz.</summary>
public sealed record QuizOptionResponse(Guid Id, string Text) : IDto;
