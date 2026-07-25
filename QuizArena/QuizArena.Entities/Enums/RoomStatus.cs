namespace QuizArena.Entities.Enums;

/// <summary>
/// Odanın yaşam döngüsü. Geçerli geçişler:
/// <c>Waiting → InProgress → Finished</c> ve her durumdan <c>Cancelled</c>.
/// </summary>
public enum RoomStatus
{
    /// <summary>Oyuncular bekleniyor; katılım ve ayrılma serbest.</summary>
    Waiting = 1,

    /// <summary>Yarışma başladı; yeni katılım kabul edilmez.</summary>
    InProgress = 2,

    /// <summary>Tüm sorular bitti; sonuçlar kesinleşti.</summary>
    Finished = 3,

    /// <summary>Kurucu iptal etti veya oda zaman aşımına düştü.</summary>
    Cancelled = 4
}
