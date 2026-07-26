using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Dtos.Questions;

/// <summary>
/// Soru + seçenekleri tek istekte oluşturur.
/// </summary>
/// <remarks>
/// Soru ve seçenekler <b>tek istekte</b> gider. Ayrı uçlardan eklenselerdi
/// "soru eklendi ama seçenekler eklenmeden istek koptu" gibi yarım kayıtlar
/// oluşur; böyle bir soru yarışmada çıktığında cevaplanamaz. Tek istek ve
/// transaction bu durumu imkânsız kılar.
/// </remarks>
public sealed record CreateQuestionRequest(
    Guid CategoryId,
    string Text,
    QuestionDifficulty Difficulty,
    int TimeLimitSeconds,
    string? Explanation,
    IReadOnlyList<SaveAnswerRequest> Answers) : IDto;

/// <summary>Soru güncelleme isteği (seçenekler tümüyle yenisiyle değiştirilir).</summary>
public sealed record UpdateQuestionRequest(
    Guid CategoryId,
    string Text,
    QuestionDifficulty Difficulty,
    int TimeLimitSeconds,
    string? Explanation,
    bool IsActive,
    IReadOnlyList<SaveAnswerRequest> Answers) : IDto;

/// <summary>Kaydedilecek seçenek.</summary>
public sealed record SaveAnswerRequest(
    string Text,
    bool IsCorrect,
    int DisplayOrder) : IDto;
