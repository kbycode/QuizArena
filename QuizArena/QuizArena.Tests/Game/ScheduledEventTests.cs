using QuizArena.Core.Entities.Concrete;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Rooms;
using QuizArena.Entities.Enums;
using QuizArena.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace QuizArena.Tests.Game;

/// <summary>
/// Zamanlanmış etkinliklerin (turnuvaların) uçtan uca testleri.
/// </summary>
/// <remarks>
/// Bu davranış gerçek zamana bağlı olduğu için <see cref="FakeClock"/> olmadan
/// test edilemezdi: "başlangıç saati gelince başlar" kuralını doğrulamak,
/// gerçek saatle beklemek anlamına gelirdi. Saat ileri sarılarak zamanlayıcının
/// kararı doğrudan ölçülüyor.
/// </remarks>
public sealed class ScheduledEventTests
{
    // =====================================================================
    //  Oluşturma ve kayıt
    // =====================================================================

    [Fact]
    public async Task Etkinlik_olusturulunca_yaklasanlar_listesinde_gorunur()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        harness.SignIn(admin);

        IDataResult<EventResponse> created = await harness.Events.CreateAsync(Request(harness));

        created.Success.Should().BeTrue();
        created.Data!.Status.Should().Be(RoomStatus.Waiting);
        created.Data.RegisteredCount.Should().Be(0, "kurucu otomatik katılımcı olmamalı");
        created.Data.CanRegister.Should().BeTrue();

        IDataResult<IReadOnlyList<EventResponse>> upcoming = await harness.Events.GetUpcomingAsync();

        upcoming.Data.Should().ContainSingle(e => e.Id == created.Data.Id);
    }

    [Fact]
    public async Task Gecmis_tarihli_etkinlik_olusturulamaz()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        harness.SignIn(admin);

        SaveEventRequest request = Request(harness) with
        {
            ScheduledStartUtc = harness.Clock.UtcNow.AddMinutes(-1)
        };

        Func<Task> act = () => harness.Events.CreateAsync(request);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Kategoride_yeterli_soru_yoksa_etkinlik_kurulmaz()
    {
        // Soru yetersizliği başlangıç anında değil, KURULUM anında yakalanmalı:
        // aksi hâlde kaydolan oyuncular etkinliğin hiç başlamadığını görürdü.
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User admin = await harness.CreateUserAsync("Yonetici");
        harness.SignIn(admin);

        SaveEventRequest request = Request(harness) with { QuestionCount = 20 };

        Func<Task> act = () => harness.Events.CreateAsync(request);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Oyuncu_kaydolup_kaydini_geri_alabilir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        User player = await harness.CreateUserAsync("Oyuncu1");

        harness.SignIn(admin);
        EventResponse created = (await harness.Events.CreateAsync(Request(harness))).Data!;

        harness.SignIn(player);
        IDataResult<EventResponse> registered = await harness.Events.RegisterAsync(created.Id);

        registered.Data!.IsRegistered.Should().BeTrue();
        registered.Data.RegisteredCount.Should().Be(1);
        registered.Data.CanRegister.Should().BeFalse("zaten kayıtlı");

        IDataResult<EventResponse> withdrawn = await harness.Events.WithdrawAsync(created.Id);

        withdrawn.Data!.IsRegistered.Should().BeFalse();
        withdrawn.Data.RegisteredCount.Should().Be(0);
        withdrawn.Data.CanRegister.Should().BeTrue("kayıt geri alındıktan sonra yeniden kaydolunabilmeli");
    }

    [Fact]
    public async Task Ayni_oyuncu_iki_kez_kaydolamaz()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        User player = await harness.CreateUserAsync("Oyuncu1");

        harness.SignIn(admin);
        EventResponse created = (await harness.Events.CreateAsync(Request(harness))).Data!;

        harness.SignIn(player);
        await harness.Events.RegisterAsync(created.Id);

        Func<Task> act = () => harness.Events.RegisterAsync(created.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Kontenjan_dolunca_kayit_reddedilir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        User first = await harness.CreateUserAsync("Oyuncu1");
        User second = await harness.CreateUserAsync("Oyuncu2");
        User third = await harness.CreateUserAsync("Oyuncu3");

        harness.SignIn(admin);
        EventResponse created = (await harness.Events.CreateAsync(Request(harness) with { MaxPlayers = 2 })).Data!;

        harness.SignIn(first);
        await harness.Events.RegisterAsync(created.Id);

        harness.SignIn(second);
        await harness.Events.RegisterAsync(created.Id);

        harness.SignIn(third);
        Func<Task> act = () => harness.Events.RegisterAsync(created.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Kontenjan_kayitli_oyuncu_sayisinin_altina_indirilemez()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        User first = await harness.CreateUserAsync("Oyuncu1");
        User second = await harness.CreateUserAsync("Oyuncu2");

        harness.SignIn(admin);
        EventResponse created = (await harness.Events.CreateAsync(Request(harness) with { MaxPlayers = 4 })).Data!;

        harness.SignIn(first);
        await harness.Events.RegisterAsync(created.Id);
        harness.SignIn(second);
        await harness.Events.RegisterAsync(created.Id);

        harness.SignIn(admin);
        Func<Task> act = () => harness.Events.UpdateAsync(created.Id, Request(harness) with { MaxPlayers = 1 });

        await act.Should().ThrowAsync<BusinessException>();
    }

    // =====================================================================
    //  Zamanlayıcı
    // =====================================================================

    [Fact]
    public async Task Saati_gelmeyen_etkinlik_baslatilmaz()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        User player = await harness.CreateUserAsync("Oyuncu1");

        harness.SignIn(admin);
        EventResponse created = (await harness.Events.CreateAsync(Request(harness))).Data!;

        harness.SignIn(player);
        await harness.Events.RegisterAsync(created.Id);

        int started = await harness.Events.ProcessDueEventsAsync();

        started.Should().Be(0);

        await using var context = harness.NewContext();
        Room room = await context.Rooms.SingleAsync(r => r.Id == created.Id);
        room.Status.Should().Be(RoomStatus.Waiting);
    }

    [Fact]
    public async Task Saati_gelen_etkinlik_otomatik_baslar_ve_kayitlilara_soru_dagitilir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        User first = await harness.CreateUserAsync("Oyuncu1");
        User second = await harness.CreateUserAsync("Oyuncu2");

        harness.SignIn(admin);
        EventResponse created = (await harness.Events.CreateAsync(Request(harness))).Data!;

        harness.SignIn(first);
        await harness.Events.RegisterAsync(created.Id);
        harness.SignIn(second);
        await harness.Events.RegisterAsync(created.Id);

        // Başlangıç saatini geç.
        harness.Clock.Advance(TimeSpan.FromHours(2));

        int started = await harness.Events.ProcessDueEventsAsync();

        started.Should().Be(1);

        await using var context = harness.NewContext();

        Room room = await context.Rooms.SingleAsync(r => r.Id == created.Id);
        room.Status.Should().Be(RoomStatus.InProgress);
        room.StartedAtUtc.Should().NotBeNull();

        // Her kayıtlı oyuncu için ayrı bir yarışma oturumu açılmalı.
        List<Competition> competitions = await context.Competitions
            .Where(c => c.RoomId == created.Id)
            .ToListAsync();

        competitions.Should().HaveCount(2);
        competitions.Should().OnlyContain(c => c.Status == CompetitionStatus.InProgress);
    }

    [Fact]
    public async Task Katilimcisiz_etkinlik_saati_gelince_iptal_edilir()
    {
        // Kimse kaydolmadıysa yarışmayı başlatmak anlamsız; etkinlik
        // "Waiting" durumunda sonsuza kadar asılı kalmamalı.
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        harness.SignIn(admin);

        EventResponse created = (await harness.Events.CreateAsync(Request(harness))).Data!;

        harness.Clock.Advance(TimeSpan.FromHours(2));

        int started = await harness.Events.ProcessDueEventsAsync();

        started.Should().Be(0);

        await using var context = harness.NewContext();
        Room room = await context.Rooms.SingleAsync(r => r.Id == created.Id);
        room.Status.Should().Be(RoomStatus.Cancelled);
    }

    [Fact]
    public async Task Baslamis_etkinlige_kaydolunamaz()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        User first = await harness.CreateUserAsync("Oyuncu1");
        User late = await harness.CreateUserAsync("GecKalan");

        harness.SignIn(admin);
        EventResponse created = (await harness.Events.CreateAsync(Request(harness))).Data!;

        harness.SignIn(first);
        await harness.Events.RegisterAsync(created.Id);

        harness.Clock.Advance(TimeSpan.FromHours(2));
        await harness.Events.ProcessDueEventsAsync();

        harness.SignIn(late);
        Func<Task> act = () => harness.Events.RegisterAsync(created.Id);

        await act.Should().ThrowAsync<BusinessException>();
    }

    // =====================================================================
    //  Diğer akışlarla etkileşim
    // =====================================================================

    [Fact]
    public async Task Etkinlik_kaydi_normal_oda_kurmayi_engellemez()
    {
        // Bu, tasarımın en kritik ayrıntısı: üç gün sonraki bir turnuvaya
        // kaydolan oyuncu, o güne kadar hiçbir oyun oynayamaz hâle gelmemeli.
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        User player = await harness.CreateUserAsync("Oyuncu1");

        harness.SignIn(admin);
        EventResponse created = (await harness.Events.CreateAsync(Request(harness))).Data!;

        harness.SignIn(player);
        await harness.Events.RegisterAsync(created.Id);

        IDataResult<RoomResponse> soloRoom = await harness.Rooms.CreateAsync(new CreateRoomRequest(
            harness.CategoryId, "Tek kişilik", RoomMode.Solo, 5, 20, 1));

        soloRoom.Success.Should().BeTrue();
        soloRoom.Data!.Status.Should().Be(RoomStatus.InProgress);
    }

    [Fact]
    public async Task Etkinlik_acik_oda_listesinde_gorunmez()
    {
        // Etkinlikler kendi listesinde gösteriliyor; "hemen katıl" listesine
        // karışsalardı oyuncu, saati gelmemiş bir odaya girip bekler kalırdı.
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User admin = await harness.CreateUserAsync("Yonetici");
        harness.SignIn(admin);

        EventResponse created = (await harness.Events.CreateAsync(Request(harness))).Data!;

        var joinable = await harness.Rooms.GetJoinableAsync(new Core.DataAccess.Paging.PageRequest());

        joinable.Data!.Items.Should().NotContain(r => r.Id == created.Id);
    }

    [Fact]
    public async Task Siradan_bir_oda_etkinlik_uclarindan_yonetilemez()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User player = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(player);

        RoomResponse room = (await harness.Rooms.CreateAsync(new CreateRoomRequest(
            harness.CategoryId, "Normal oda", RoomMode.PublicMultiplayer, 5, 20, 4))).Data!;

        Func<Task> act = () => harness.Events.RegisterAsync(room.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Zamanlanmis_baslatma_siradan_odada_calismaz()
    {
        // StartScheduledEventAsync kurucu kontrolü yapmıyor; kapsamının
        // yalnızca etkinliklerle sınırlı kaldığını doğruluyoruz.
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 10);
        User host = await harness.CreateUserAsync("Kurucu");
        harness.SignIn(host);

        RoomResponse room = (await harness.Rooms.CreateAsync(new CreateRoomRequest(
            harness.CategoryId, "Normal oda", RoomMode.PublicMultiplayer, 5, 20, 4))).Data!;

        Func<Task> act = () => harness.Rooms.StartScheduledEventAsync(room.Id);

        await act.Should().ThrowAsync<BusinessException>();
    }

    // =====================================================================
    //  Yardımcı
    // =====================================================================

    private static SaveEventRequest Request(GameTestHarness harness) => new(
        harness.CategoryId,
        "Cuma Turnuvası",
        "Haftalık genel kültür turnuvası.",
        harness.Clock.UtcNow.AddHours(1),
        QuestionCount: 5,
        SecondsPerQuestion: 20,
        MaxPlayers: 8);
}
