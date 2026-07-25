using System.Security.Cryptography;

namespace QuizArena.Core.Utilities.Ids;

/// <summary>
/// SQL Server'ın <c>uniqueidentifier</c> sıralamasına göre artan GUID üretir.
/// </summary>
/// <remarks>
/// SQL Server bir GUID'i karşılaştırırken bayt sırasını soldan sağa değil,
/// son 6 baytı (Data4'ün son yarısı) en anlamlı bölüm kabul ederek okur.
/// Bu yüzden "zaman damgasını sona koymak" artan anahtar üretmenin yoludur:
/// böylece her <c>INSERT</c> kümelenmiş indeksin sonuna düşer, sayfa bölünmesi
/// (page split) ve indeks fragmentasyonu oluşmaz.
/// <para>
/// İlk 10 bayt kriptografik olarak rastgeledir; yani ID tahmin edilemez
/// (kaynak numaralandırma / IDOR saldırılarına karşı sıralı <c>int</c>'ten
/// güvenlidir), buna karşılık indeks davranışı sıralı kalır.
/// </para>
/// </remarks>
public static class SequentialGuid
{
    public static Guid NewGuid()
    {
        Span<byte> bytes = stackalloc byte[16];

        // 0..9  -> rastgele: tahmin edilemezlik
        RandomNumberGenerator.Fill(bytes[..10]);

        // 10..15 -> zaman damgası (big-endian): SQL Server'ın sıralama gördüğü bölüm
        long ticks = DateTime.UtcNow.Ticks;
        bytes[10] = (byte)(ticks >> 40);
        bytes[11] = (byte)(ticks >> 32);
        bytes[12] = (byte)(ticks >> 24);
        bytes[13] = (byte)(ticks >> 16);
        bytes[14] = (byte)(ticks >> 8);
        bytes[15] = (byte)ticks;

        return new Guid(bytes);
    }
}
