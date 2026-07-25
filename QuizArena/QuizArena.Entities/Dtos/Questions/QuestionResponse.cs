using QuizArena.Core.Entities;
using QuizArena.Entities.Enums;

namespace QuizArena.Entities.Dtos.Questions;

/// <summary>
/// Soru — <b>yönetim</b> görünümü. Doğru cevap bilgisini içerir.
/// </summary>
/// <remarks>
/// Bu DTO yalnızca <c>Question.Manage</c> yetkisi olan uçlardan döner.
/// Oyuncuya soru servis ederken kullanılan DTO
/// <c>Dtos.Play.QuizQuestionResponse</c>'tur ve orada doğru cevap alanı
/// <b>hiç tanımlı değildir</b>. İki ayrı tip kullanmak, "yanlış yerde yanlış
/// DTO döndürme" hatasını derleme zamanında görünür kılar.
/// </remarks>
public sealed record QuestionResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Text,
    QuestionDifficulty Difficulty,
    int TimeLimitSeconds,
    string? Explanation,
    bool IsActive,
    int TimesAsked,
    int TimesAnsweredCorrectly,
    IReadOnlyList<QuestionAnswerResponse> Answers) : IDto;

/// <summary>Yönetim görünümünde bir seçenek (doğruluk bilgisiyle).</summary>
public sealed record QuestionAnswerResponse(
    Guid Id,
    string Text,
    bool IsCorrect,
    int DisplayOrder) : IDto;
