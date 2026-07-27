namespace QuizArena.DAL.Seed;

/// <summary>
/// Başlangıç verisi ayarları.
/// </summary>
/// <remarks>
/// <b>Yönetici parolası kaynak kodda değildir.</b> Bu, hazır şablonlarda en sık
/// görülen güvenlik hatasıdır: <c>Admin123!</c> gibi bir parola koda gömülür,
/// proje GitHub'a yüklenir ve üretime çıkan her kopyada aynı parola geçerli
/// olur. Değer buradan (ortam değişkeni / user-secrets) okunur:
/// <code>
/// dotnet user-secrets set "Seed:AdminPassword" "…"
/// # veya ortam değişkeni:
/// Seed__AdminPassword=…
/// </code>
/// Parola verilmezse: Geliştirme ortamında rastgele üretilip <b>bir kez</b>
/// loga yazılır; Üretim ortamında yönetici hesabı <b>hiç oluşturulmaz</b>
/// (bkz. <see cref="DatabaseSeeder"/>).
/// </remarks>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; set; } = "admin@quizarena.local";

    public string AdminNickname { get; set; } = "Yonetici";

    public string? AdminPassword { get; set; }

    /// <summary>
    /// Sıralama tablosunun boş görünmemesi için örnek oyuncu ve istatistik
    /// üretilsin mi? Yalnızca geliştirme/demo ortamında açılmalıdır.
    /// </summary>
    public bool CreateDemoPlayers { get; set; }

    /// <summary>
    /// Başlangıç içeriğinin dili: <c>tr</c> veya <c>en</c>.
    /// </summary>
    /// <remarks>
    /// <b>İçerik dili, arayüz dilinden ayrıdır.</b> Arayüz her istekte
    /// <c>Accept-Language</c> ile değişebilir; soru havuzu ise veritabanına
    /// bir kez yazılır. İki dilin soruları bir arada yüklenseydi oyuncu, kendi
    /// dilinde olmayan sorularla karşılaşırdı — bu yüzden tek set seçilir.
    /// <para>
    /// Yalnızca <b>boş</b> bir veritabanında etkilidir; seed idempotent
    /// çalıştığı için var olan içeriği çevirmez.
    /// </para>
    /// </remarks>
    public string ContentLanguage { get; set; } = "tr";

    /// <summary>Seçilen içerik dili İngilizce mi?</summary>
    public bool UseEnglishContent =>
        ContentLanguage.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// <see cref="AdminPassword"/> verilmediğinde rastgele bir parola üretilip
    /// loga yazılmasına izin verilsin mi?
    /// </summary>
    /// <remarks>
    /// Bu bayrağı ortamı bilen taraf (API projesi) ayarlar; veri erişim katmanı
    /// "geliştirme ortamında mıyım?" sorusunu sormak zorunda kalmaz —
    /// böylece DAL, barındırma (hosting) soyutlamalarına bağımlı olmaz.
    /// Üretimde <c>false</c> kalır ve parola yoksa yönetici hesabı hiç
    /// oluşturulmaz.
    /// </remarks>
    public bool AllowGeneratedAdminPassword { get; set; }
}
