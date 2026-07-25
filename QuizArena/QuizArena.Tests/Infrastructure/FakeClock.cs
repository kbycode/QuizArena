using QuizArena.Core.Utilities.Clock;

namespace QuizArena.Tests.Infrastructure;

/// <summary>
/// Testlerde zamanı kontrol etmeye yarayan saat.
/// </summary>
/// <remarks>
/// <see cref="IClock"/> soyutlamasının varlık sebebi bu sınıf: "oyuncu 12
/// saniyede cevapladı" senaryosunu gerçekten 12 saniye bekleyerek test etmek
/// hem yavaş hem de güvenilmez olurdu. Burada saati istediğimiz kadar
/// ileri sarabiliyoruz.
/// </remarks>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTime? startUtc = null)
        => UtcNow = startUtc ?? new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public DateTime UtcNow { get; private set; }

    public void Advance(TimeSpan amount) => UtcNow = UtcNow.Add(amount);

    public void AdvanceSeconds(double seconds) => Advance(TimeSpan.FromSeconds(seconds));

    public void AdvanceMilliseconds(double milliseconds) => Advance(TimeSpan.FromMilliseconds(milliseconds));
}
