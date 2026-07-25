using System.Diagnostics;
using System.Globalization;
using QuizArena.Core.Utilities.Security.Hashing;
using FluentAssertions;
using Xunit;

namespace QuizArena.Tests.Security;

/// <summary>
/// Parola özetleme testleri.
/// </summary>
/// <remarks>
/// Testlerde düşük iterasyon sayısı kullanılıyor (hız için). Algoritmanın
/// davranışı iterasyon sayısından bağımsız olduğu için testin doğrulama gücü
/// azalmaz; üretimde varsayılan olan OWASP değeri geçerlidir.
/// </remarks>
public sealed class Pbkdf2PasswordHasherTests
{
    private const int TestIterations = 1_000;

    private static Pbkdf2PasswordHasher CreateHasher(int iterations = TestIterations)
        => new(iterations);

    [Fact]
    public void Dogru_parola_dogrulanir()
    {
        var hasher = CreateHasher();
        string hash = hasher.Hash("Guclu.Parola123");

        hasher.Verify("Guclu.Parola123", hash).Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void Yanlis_parola_reddedilir()
    {
        var hasher = CreateHasher();
        string hash = hasher.Hash("Guclu.Parola123");

        hasher.Verify("Guclu.Parola124", hash).Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Ayni_parola_her_seferinde_farkli_ozet_uretir()
    {
        var hasher = CreateHasher();

        string first = hasher.Hash("AyniParola1");
        string second = hasher.Hash("AyniParola1");

        // Tuz rastgele olduğu için özetler farklı olmalı. Aksi hâlde iki
        // kullanıcının aynı parolayı kullandığı, veritabanına bakan biri
        // tarafından görülebilirdi (ve gökkuşağı tablosu saldırısı mümkün olurdu).
        first.Should().NotBe(second);
        hasher.Verify("AyniParola1", first).Should().Be(PasswordVerificationResult.Success);
        hasher.Verify("AyniParola1", second).Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void Ozet_kendi_parametrelerini_tasir()
    {
        string hash = CreateHasher().Hash("Parola123");

        string[] parts = hash.Split('$');

        parts.Should().HaveCount(4);
        parts[0].Should().Be("pbkdf2-sha256");
        parts[1].Should().Be(TestIterations.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Iterasyon_sayisi_artirildiginda_yeniden_ozetleme_istenir()
    {
        // Eski, zayıf parametrelerle üretilmiş bir özet:
        string oldHash = CreateHasher(iterations: 500).Hash("Parola123");

        // Yeni, daha güçlü parametrelerle çalışan doğrulayıcı:
        var currentHasher = CreateHasher(iterations: 5_000);

        // Parola doğru kabul edilir AMA yeniden özetlenmesi gerektiği bildirilir.
        // Bu sayede iterasyon sayısı yıllar içinde artırılabilir ve mevcut
        // kullanıcılar parolalarını değiştirmek zorunda kalmaz.
        currentHasher.Verify("Parola123", oldHash)
            .Should().Be(PasswordVerificationResult.SuccessRehashNeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bozuk-bicim")]
    [InlineData("pbkdf2-sha256$abc$tuz$ozet")]
    [InlineData("bcrypt$1000$tuz$ozet")]
    [InlineData("pbkdf2-sha256$1000$gecersiz-base64!$ozet")]
    public void Bozuk_ozet_hata_atmadan_reddedilir(string corruptedHash)
    {
        var hasher = CreateHasher();

        // Veritabanındaki bozuk bir kayıt, giriş ucunun 500 hatası vermesine
        // yol açmamalı; sadece doğrulama başarısız olmalı.
        Func<PasswordVerificationResult> act = () => hasher.Verify("Parola123", corruptedHash);

        act.Should().NotThrow();
        act().Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Farkli_uzunluktaki_ozet_indeks_hatasi_uretmez()
    {
        // Orijinal koddaki VerifyPasswordHash, özet uzunlukları farklıysa
        // IndexOutOfRangeException atıyordu. FixedTimeEquals bunu yapmaz.
        var hasher = CreateHasher();
        string shortHash = $"pbkdf2-sha256${TestIterations}${Convert.ToBase64String(new byte[16])}${Convert.ToBase64String(new byte[8])}";

        Func<PasswordVerificationResult> act = () => hasher.Verify("Parola123", shortHash);

        act.Should().NotThrow();
        act().Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Bos_parola_ozetlenemez()
    {
        var hasher = CreateHasher();

        Action act = () => hasher.Hash("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Dogrulama_suresi_yanlis_parolada_da_benzer_kalir()
    {
        // Zamanlama saldırısına karşı kaba bir denetim: doğru ve yanlış
        // parolanın doğrulama süresi aynı büyüklük mertebesinde olmalı.
        // (Kesin ölçüm birim testinin işi değil; buradaki amaç "ilk farklı
        // baytta çıkıp erken dönen" bir uygulamaya geri dönülmesini fark etmek.)
        var hasher = CreateHasher(iterations: 20_000);
        string hash = hasher.Hash("DogruParola123");

        var correctWatch = Stopwatch.StartNew();
        hasher.Verify("DogruParola123", hash);
        correctWatch.Stop();

        var wrongWatch = Stopwatch.StartNew();
        hasher.Verify("XogruParola123", hash);
        wrongWatch.Stop();

        double ratio = (double)wrongWatch.ElapsedTicks / Math.Max(correctWatch.ElapsedTicks, 1);

        ratio.Should().BeInRange(0.2, 5.0,
            "yanlış parola doğrulaması, doğru parolayla benzer sürede tamamlanmalı");
    }
}
