namespace QuizArena.Entities.Enums;

/// <summary>Yarışma oturumunun durumu.</summary>
public enum CompetitionStatus
{
    InProgress = 1,

    /// <summary>Tüm sorular cevaplandı/süresi doldu ve sonuç hesaplandı.</summary>
    Completed = 2,

    /// <summary>Yarışma yarıda bırakıldı.</summary>
    Abandoned = 3
}
