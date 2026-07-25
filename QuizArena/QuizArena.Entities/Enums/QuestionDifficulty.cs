namespace QuizArena.Entities.Enums;

/// <summary>
/// Soru zorluğu. Puanlamada çarpan olarak kullanılır.
/// </summary>
/// <remarks>
/// Projenin ilk hâlinde bu tür ayrımlar <c>int Type</c> alanlarıyla
/// tutuluyordu. <c>room.Type = 2</c> satırını okuyan hiç kimse 2'nin ne
/// olduğunu bilemez; enum ise hem kendini belgeler hem de derleyicinin
/// geçersiz değeri yakalamasına imkân verir.
/// </remarks>
public enum QuestionDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}
