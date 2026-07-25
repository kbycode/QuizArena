namespace QuizArena.Core.Exceptions;

/// <summary>İstenen kayıt yok. → HTTP 404.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }

    /// <summary>
    /// "Kategori bulunamadı." gibi tutarlı mesaj üretir.
    /// Mesaja kimlik (id) <b>bilinçli olarak</b> eklenmez: hangi id'nin var
    /// olduğu bilgisi kaynak numaralandırma (enumeration) saldırısına yardım eder.
    /// </summary>
    public static NotFoundException For(string entityDisplayName)
        => new($"{entityDisplayName} bulunamadı.");
}
