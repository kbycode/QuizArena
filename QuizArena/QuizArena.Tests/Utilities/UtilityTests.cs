using QuizArena.BLL.Constants;
using QuizArena.BLL.Utilities;
using QuizArena.Core.DataAccess.Paging;
using QuizArena.Core.Utilities.Ids;
using FluentAssertions;
using Xunit;

namespace QuizArena.Tests.Utilities;

public sealed class SlugGeneratorTests
{
    [Theory]
    [InlineData("Genel Kültür", "genel-kultur")]
    [InlineData("Bilim ve Teknoloji", "bilim-ve-teknoloji")]
    [InlineData("Sanat & Edebiyat", "sanat-edebiyat")]
    [InlineData("  Boşluklu   Ad  ", "bosluklu-ad")]
    [InlineData("Şişli Çağrı Ölçüm İğne", "sisli-cagri-olcum-igne")]
    public void Turkce_karakterler_dogru_donusturulur(string input, string expected)
    {
        SlugGenerator.Generate(input).Should().Be(expected);
    }

    [Fact]
    public void Buyuk_I_harfi_dotless_i_ile_karistirilmaz()
    {
        // Klasik "Turkish i" tuzağı: normalizasyon tabanlı yaklaşımlar
        // 'ı' harfini yok sayar ya da 'I' harfini 'ı' yapar.
        SlugGenerator.Generate("Işık").Should().Be("isik");
        SlugGenerator.Generate("İzmir").Should().Be("izmir");
    }

    [Fact]
    public void Bastaki_ve_sondaki_tireler_temizlenir()
    {
        SlugGenerator.Generate("!!! Tarih !!!").Should().Be("tarih");
    }

    [Fact]
    public void Yalnizca_sembolden_olusan_ad_bos_slug_uretir()
    {
        // CategoryManager bu durumu görüp "kategori" varsayılanını kullanıyor.
        SlugGenerator.Generate("###").Should().BeEmpty();
    }

    [Fact]
    public void Bos_metin_hata_atar()
    {
        Action act = () => SlugGenerator.Generate("   ");

        act.Should().Throw<ArgumentException>();
    }
}

public sealed class JoinCodeGeneratorTests
{
    [Fact]
    public void Kod_beklenen_uzunlukta_uretilir()
    {
        JoinCodeGenerator.Generate().Should().HaveLength(GameRules.JoinCodeLength);
    }

    [Fact]
    public void Kodda_karistirilabilir_karakter_bulunmaz()
    {
        // 0/O, 1/I/l, 2/Z, 5/S, 8/B çiftleri telefonda okunurken karışıyor.
        var codes = Enumerable.Range(0, 500).Select(_ => JoinCodeGenerator.Generate()).ToArray();

        codes.Should().OnlyContain(code => !code.Any(c => "01258BIOSZ".Contains(c)));
    }

    [Fact]
    public void Uretilen_kodlar_yeterince_dagilir()
    {
        // Kriptografik rastgelelik kullanıldığı için 500 kodda çakışma
        // beklenmiyor (26^6 ≈ 309 milyon olasılık).
        var codes = Enumerable.Range(0, 500).Select(_ => JoinCodeGenerator.Generate()).ToArray();

        codes.Distinct().Should().HaveCount(codes.Length);
    }
}

public sealed class SequentialGuidTests
{
    [Fact]
    public void Uretilen_kimlikler_tekildir()
    {
        var ids = Enumerable.Range(0, 2_000).Select(_ => SequentialGuid.NewGuid()).ToArray();

        ids.Distinct().Should().HaveCount(ids.Length);
    }

    [Fact]
    public void Kimlikler_SQL_Server_siralamasina_gore_artar()
    {
        // SQL Server uniqueidentifier'ı son 6 bayta göre sıralar; kümelenmiş
        // indeksin sonuna yazılabilmesi için bu bölüm artan olmalı.
        var ids = new List<Guid>();
        for (var i = 0; i < 50; i++)
        {
            ids.Add(SequentialGuid.NewGuid());
            Thread.Sleep(1); // zaman damgasının ilerlediğinden emin ol
        }

        var sortedBySqlServerRules = ids.OrderBy(id => id, new SqlServerGuidComparer()).ToList();

        sortedBySqlServerRules.Should().Equal(ids);
    }

    /// <summary>SQL Server'ın <c>uniqueidentifier</c> karşılaştırma sırası.</summary>
    private sealed class SqlServerGuidComparer : IComparer<Guid>
    {
        private static readonly int[] SqlServerByteOrder = [10, 11, 12, 13, 14, 15, 8, 9, 6, 7, 4, 5, 0, 1, 2, 3];

        public int Compare(Guid x, Guid y)
        {
            byte[] left = x.ToByteArray();
            byte[] right = y.ToByteArray();

            foreach (int index in SqlServerByteOrder)
            {
                int comparison = left[index].CompareTo(right[index]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }
    }
}

public sealed class PageRequestTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void Sayfa_numarasi_bire_kirpilir(int requested, int expected)
    {
        new PageRequest { Page = requested }.Page.Should().Be(expected);
    }

    [Fact]
    public void Sayfa_boyutu_ust_sinirla_kirpilir()
    {
        // İstemci "pageSize=1000000" göndererek tüm tabloyu çekemez:
        // bu yalnızca yavaşlık değil, hizmet dışı bırakma (DoS) riskidir.
        new PageRequest { PageSize = 1_000_000 }.PageSize.Should().Be(PageRequest.MaxPageSize);
    }

    [Fact]
    public void Gecersiz_sayfa_boyutu_varsayilana_doner()
    {
        new PageRequest { PageSize = 0 }.PageSize.Should().Be(PageRequest.DefaultPageSize);
    }

    [Fact]
    public void Atlanacak_kayit_sayisi_dogru_hesaplanir()
    {
        new PageRequest { Page = 3, PageSize = 20 }.Skip.Should().Be(40);
    }
}
