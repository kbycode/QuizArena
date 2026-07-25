namespace QuizArena.Core.Utilities.Clock;

/// <inheritdoc cref="IClock"/>
public sealed class SystemClock : IClock
{
    /// <summary>
    /// Her zaman UTC. Yerel saat kullanılmaz: sunucu saat dilimi değişse veya
    /// yaz saati uygulaması geçişi yaşansa bile yarışma süreleri kaymaz.
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}
