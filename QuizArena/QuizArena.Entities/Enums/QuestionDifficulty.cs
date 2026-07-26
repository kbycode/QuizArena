namespace QuizArena.Entities.Enums;

/// <summary>
/// Soru zorluğu. Puanlamada çarpan olarak kullanılır.
/// </summary>
/// <remarks>
/// Zorluk <c>int</c> değil <b>enum</b>: <c>question.Type = 2</c> satırını
/// okuyan hiç kimse 2'nin ne olduğunu bilemez. Enum hem kendini belgeler hem
/// de geçersiz değeri derleyiciye yakalatır.
/// </remarks>
public enum QuestionDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}
