namespace QuizArena.Core.Utilities.Clock;

/// <summary>
/// Sistem saatinin soyutlaması.
/// <para>
/// Neden doğrudan <c>DateTime.UtcNow</c> çağırmıyoruz: yarışmadaki süre/skor
/// hesabı zamana bağlı. Zamanı enjekte edilebilir yapmadan "12 saniyede cevap
/// verildi" senaryosunu birim testinde deterministik olarak kuramazsınız.
/// Testlerde <c>FakeClock</c> ile saat ileri sarılır.
/// </para>
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
