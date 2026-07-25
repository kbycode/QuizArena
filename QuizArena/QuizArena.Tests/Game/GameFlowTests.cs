using QuizArena.Core.Entities.Concrete;
using QuizArena.Core.Exceptions;
using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Play;
using QuizArena.Entities.Dtos.Rooms;
using QuizArena.Entities.Enums;
using QuizArena.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace QuizArena.Tests.Game;

/// <summary>
/// Yarışma akışının uçtan uca testleri (gerçek repository + gerçek SQLite).
/// </summary>
/// <remarks>
/// Bu testler sahte repository kullanmıyor; asıl amaç veri modeliyle iş
/// kurallarının <b>birlikte</b> doğru çalıştığını göstermek. Tekil indeksler,
/// yabancı anahtarlar ve sorgu filtreleri de test kapsamında.
/// </remarks>
public sealed class GameFlowTests
{
    // =====================================================================
    //  Mutlu yol
    // =====================================================================

    [Fact]
    public async Task Tek_kisilik_oda_kurulunca_yarisma_hemen_baslar()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);

        IDataResult<RoomResponse> room = await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        room.Success.Should().BeTrue();
        room.Data!.Status.Should().Be(RoomStatus.InProgress);
        room.Data.JoinCode.Should().NotBeNullOrWhiteSpace();

        // İlk soru hemen alınabilir olmalı: oyuncu ekstra bir adım atmadan oynar.
        IDataResult<QuizQuestionResponse?> question = await harness.Game.GetCurrentQuestionAsync();

        question.Data.Should().NotBeNull();
        question.Data!.Order.Should().Be(1);
        question.Data.TotalQuestions.Should().Be(5);
        question.Data.Options.Should().HaveCount(4);
    }

    [Fact]
    public async Task Soru_sunuldugunda_sunucu_zaman_damgalarini_yazar()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync();
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        IDataResult<QuizQuestionResponse?> question = await harness.Game.GetCurrentQuestionAsync();

        await using var context = harness.NewContext();
        CompetitionQuestion stored = await context.CompetitionQuestions
            .SingleAsync(cq => cq.Id == question.Data!.CompetitionQuestionId);

        // Süre ölçümünün tek doğruluk kaynağı sunucu: istemci hiçbir zaman
        // "kaç saniyede cevapladım" bilgisi göndermiyor.
        stored.AskedAtUtc.Should().Be(harness.Clock.UtcNow);
        stored.ClosesAtUtc.Should().Be(harness.Clock.UtcNow.AddSeconds(20));
    }

    [Fact]
    public async Task Dogru_cevap_puan_kazandirir_ve_seriyi_artirir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync();
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        AnswerResultResponse first = await AnswerAsync(harness, correct: true, afterSeconds: 2);

        first.IsCorrect.Should().BeTrue();
        first.PointsBreakdown.Total.Should().BeGreaterThan(0);
        first.PointsBreakdown.SpeedBonus.Should().BeGreaterThan(0, "hızlı cevap ikramiye almalı");
        first.PointsBreakdown.StreakBonus.Should().Be(0, "ilk doğru cevap seri ikramiyesi almaz");
        first.CurrentStreak.Should().Be(1);

        AnswerResultResponse second = await AnswerAsync(harness, correct: true, afterSeconds: 2);

        second.CurrentStreak.Should().Be(2);
        second.PointsBreakdown.StreakBonus.Should().BeGreaterThan(0, "seri sürdükçe ikramiye gelir");
        second.TotalScore.Should().BeGreaterThan(first.TotalScore);
    }

    [Fact]
    public async Task Yanlis_cevap_puan_getirmez_ve_seriyi_sifirlar()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync();
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        AnswerResultResponse correct = await AnswerAsync(harness, correct: true, afterSeconds: 1);
        AnswerResultResponse wrong = await AnswerAsync(harness, correct: false, afterSeconds: 1);

        wrong.IsCorrect.Should().BeFalse();
        wrong.PointsBreakdown.Total.Should().Be(0);
        wrong.CurrentStreak.Should().Be(0);
        wrong.TotalScore.Should().Be(correct.TotalScore, "yanlış cevap toplam puanı değiştirmez");

        // Doğru cevap yalnızca cevap gönderildikten SONRA açıklanır.
        wrong.CorrectAnswerId.Should().NotBeEmpty();
        wrong.CorrectAnswerText.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Tum_sorular_cevaplaninca_yarisma_tamamlanir_ve_istatistik_islenir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        AnswerResultResponse? last = null;
        for (var i = 0; i < 5; i++)
        {
            last = await AnswerAsync(harness, correct: true, afterSeconds: 1);
        }

        last!.HasNextQuestion.Should().BeFalse();

        await using var context = harness.NewContext();

        Competition competition = await context.Competitions.SingleAsync();
        competition.Status.Should().Be(CompetitionStatus.Completed);
        competition.CorrectCount.Should().Be(5);
        competition.FinishedAtUtc.Should().NotBeNull();
        competition.AccuracyPercentage.Should().Be(100);

        UserStatistic statistic = await context.UserStatistics.SingleAsync(s => s.UserId == user.Id);
        statistic.TotalCompetitions.Should().Be(1);
        statistic.TotalCorrectAnswers.Should().Be(5);
        statistic.TotalScore.Should().Be(competition.TotalScore);
        statistic.BestStreak.Should().Be(5);

        // Oda da kapanmış olmalı: tek katılımcı bitti.
        Room room = await context.Rooms.SingleAsync();
        room.Status.Should().Be(RoomStatus.Finished);
    }

    [Fact]
    public async Task Kusursuz_yarisma_rozet_kazandirir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        AnswerResultResponse? last = null;
        for (var i = 0; i < 5; i++)
        {
            // 1 saniyede cevap → hem "Kusursuz" hem "Şimşek" rozeti beklenir.
            last = await AnswerAsync(harness, correct: true, afterSeconds: 1);
        }

        IDataResult<CompetitionSummaryResponse> summary =
            await harness.Game.GetSummaryAsync(last!.CompetitionId);

        summary.Data!.NewAchievements.Select(a => a.Code)
            .Should().Contain(
            [
                AchievementCode.FirstBlood,
                AchievementCode.Perfectionist,
                AchievementCode.QuickThinker
            ]);
    }

    // =====================================================================
    //  Hile ve kötüye kullanım senaryoları
    // =====================================================================

    [Fact]
    public async Task Ayni_soruya_ikinci_cevap_reddedilir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync();
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        QuizQuestionResponse question = (await harness.Game.GetCurrentQuestionAsync()).Data!;
        Guid answerId = question.Options[0].Id;

        await harness.Game.SubmitAnswerAsync(new SubmitAnswerRequest(question.CompetitionQuestionId, answerId));

        // İkinci gönderim puanı ikiye katlamaya çalışan bir istektir.
        Func<Task> act = () => harness.Game.SubmitAnswerAsync(
            new SubmitAnswerRequest(question.CompetitionQuestionId, answerId));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Baskasinin_sorusuna_cevap_gonderilemez()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync();

        User owner = await harness.CreateUserAsync("Sahibi");
        User attacker = await harness.CreateUserAsync("Saldirgan");

        harness.SignIn(owner);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));
        QuizQuestionResponse question = (await harness.Game.GetCurrentQuestionAsync()).Data!;

        // Saldırgan, kurbanın soru kimliğini ele geçirmiş olsa bile
        // (IDOR denemesi) cevap gönderemez.
        harness.SignIn(attacker);

        Func<Task> act = () => harness.Game.SubmitAnswerAsync(
            new SubmitAnswerRequest(question.CompetitionQuestionId, question.Options[0].Id));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Baska_soruya_ait_sik_reddedilir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        QuizQuestionResponse question = (await harness.Game.GetCurrentQuestionAsync()).Data!;

        await using var context = harness.NewContext();
        Guid foreignAnswerId = await context.Answers
            .Where(a => a.QuestionId != context.CompetitionQuestions
                .Where(cq => cq.Id == question.CompetitionQuestionId)
                .Select(cq => cq.QuestionId)
                .First())
            .Select(a => a.Id)
            .FirstAsync();

        // Şık kimliklerini deneyerek doğru cevabı arama girişimi.
        Func<Task> act = () => harness.Game.SubmitAnswerAsync(
            new SubmitAnswerRequest(question.CompetitionQuestionId, foreignAnswerId));

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Sunulmamis_soruya_cevap_gonderilemez()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        // İlk soruyu al (sunulmuş olur), ama İKİNCİ soruyu cevaplamayı dene.
        await harness.Game.GetCurrentQuestionAsync();

        await using var context = harness.NewContext();
        CompetitionQuestion notServedYet = await context.CompetitionQuestions
            .Where(cq => cq.AskedAtUtc == null)
            .OrderBy(cq => cq.Order)
            .FirstAsync();

        Guid anyAnswerId = await context.Answers
            .Where(a => a.QuestionId == notServedYet.QuestionId)
            .Select(a => a.Id)
            .FirstAsync();

        // Soruları toptan cevaplayarak süre ikramiyesini sömürme girişimi.
        Func<Task> act = () => harness.Game.SubmitAnswerAsync(
            new SubmitAnswerRequest(notServedYet.Id, anyAnswerId));

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Sure_dolduktan_sonra_verilen_cevap_puan_getirmez()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync();
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        QuizQuestionResponse question = (await harness.Game.GetCurrentQuestionAsync()).Data!;

        // Süre limiti 20 saniye; toleransın da ötesine geçiyoruz.
        harness.Clock.AdvanceSeconds(25);

        IDataResult<AnswerResultResponse> result = await harness.Game.SubmitAnswerAsync(
            new SubmitAnswerRequest(question.CompetitionQuestionId, question.Options[0].Id));

        result.Data!.IsTimedOut.Should().BeTrue();
        result.Data.IsCorrect.Should().BeFalse("süre dolduktan sonra doğru şık bile puan getirmez");
        result.Data.PointsBreakdown.Total.Should().Be(0);
    }

    [Fact]
    public async Task Sure_asimi_toleransi_icindeki_cevap_kabul_edilir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync();
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        QuizQuestionResponse question = (await harness.Game.GetCurrentQuestionAsync()).Data!;

        // Son saniyede verilen cevabın ağ gecikmesi yüzünden kaybedilmemesi
        // için 1,5 saniyelik tolerans var.
        harness.Clock.AdvanceMilliseconds(20_000 + 800);

        IDataResult<AnswerResultResponse> result = await harness.Game.SubmitAnswerAsync(
            new SubmitAnswerRequest(question.CompetitionQuestionId, question.Options[0].Id));

        result.Data!.IsTimedOut.Should().BeFalse();
        result.Data.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task Cevaplanmayan_soru_sonraki_istekte_otomatik_kapatilir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 3);
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 3));

        QuizQuestionResponse first = (await harness.Game.GetCurrentQuestionAsync()).Data!;

        // Oyuncu sekmeyi kapattı, süre geçti, sonra geri döndü.
        harness.Clock.AdvanceSeconds(60);

        QuizQuestionResponse? next = (await harness.Game.GetCurrentQuestionAsync()).Data;

        next.Should().NotBeNull();
        next!.Order.Should().Be(2, "süresi geçen soru kapatılıp sıradakine geçilmeli");

        await using var context = harness.NewContext();
        CompetitionAnswer timeoutAnswer = await context.CompetitionAnswers
            .SingleAsync(ca => ca.CompetitionQuestionId == first.CompetitionQuestionId);

        timeoutAnswer.IsTimedOut.Should().BeTrue();
        timeoutAnswer.SelectedAnswerId.Should().BeNull();
        timeoutAnswer.EarnedPoints.Should().Be(0);
    }

    [Fact]
    public async Task Yarim_birakilan_yarisma_istatistige_islenmez()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        await AnswerAsync(harness, correct: true, afterSeconds: 1);
        await harness.Game.AbandonAsync();

        await using var context = harness.NewContext();

        Competition competition = await context.Competitions.SingleAsync();
        competition.Status.Should().Be(CompetitionStatus.Abandoned);

        // "Kötü gidiyor" diyerek çıkmak, doğruluk oranını korumanın yolu olmamalı.
        UserStatistic statistic = await context.UserStatistics.SingleAsync(s => s.UserId == user.Id);
        statistic.TotalCompetitions.Should().Be(0);
        statistic.TotalScore.Should().Be(0);
    }

    [Fact]
    public async Task Devam_eden_yarisma_yokken_soru_istenemez()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync();
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);

        Func<Task> act = () => harness.Game.GetCurrentQuestionAsync();

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Baskasinin_yarisma_sonucu_goruntulenemez()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 1);

        User owner = await harness.CreateUserAsync("Sahibi");
        User other = await harness.CreateUserAsync("Digeri");

        harness.SignIn(owner);
        await harness.Rooms.CreateAsync(SoloRequest(harness, 1));
        AnswerResultResponse result = await AnswerAsync(harness, correct: true, afterSeconds: 1);

        harness.SignIn(other);

        Func<Task> act = () => harness.Game.GetSummaryAsync(result.CompetitionId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // =====================================================================
    //  Oda kuralları
    // =====================================================================

    [Fact]
    public async Task Yetersiz_soru_varsa_oda_kurulamaz()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);

        // Kategoride 5 soru var, 20 soruluk yarışma isteniyor.
        Func<Task> act = () => harness.Rooms.CreateAsync(SoloRequest(harness, 20));

        // Hata, arkadaşlar davet edildikten sonra değil, oda kurulurken verilir.
        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Ayni_kullanici_ikinci_bir_odaya_sahip_olamaz()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User user = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(user);

        await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        Func<Task> act = () => harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Tek_kisilik_odaya_katilim_yapilamaz()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);

        User host = await harness.CreateUserAsync("Kurucu");
        User guest = await harness.CreateUserAsync("Misafir");

        harness.SignIn(host);
        IDataResult<RoomResponse> room = await harness.Rooms.CreateAsync(SoloRequest(harness, 5));

        harness.SignIn(guest);

        Func<Task> act = () => harness.Rooms.JoinAsync(new JoinRoomRequest(room.Data!.JoinCode!));

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Yarismayi_yalnizca_kurucu_baslatabilir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);

        User host = await harness.CreateUserAsync("Kurucu");
        User guest = await harness.CreateUserAsync("Misafir");

        harness.SignIn(host);
        IDataResult<RoomResponse> room = await harness.Rooms.CreateAsync(
            new CreateRoomRequest(harness.CategoryId, "Ortak oda", RoomMode.PrivateMultiplayer, 5, 20, 4));

        harness.SignIn(guest);
        await harness.Rooms.JoinAsync(new JoinRoomRequest(room.Data!.JoinCode!));

        Func<Task> act = () => harness.Rooms.StartAsync(room.Data.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Cok_oyunculu_yarismada_tum_oyuncular_ayni_sorulari_ayni_sirada_gorur()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);

        User host = await harness.CreateUserAsync("Kurucu");
        User guest = await harness.CreateUserAsync("Misafir");

        harness.SignIn(host);
        IDataResult<RoomResponse> room = await harness.Rooms.CreateAsync(
            new CreateRoomRequest(harness.CategoryId, "Ortak oda", RoomMode.PrivateMultiplayer, 5, 20, 4));

        harness.SignIn(guest);
        await harness.Rooms.JoinAsync(new JoinRoomRequest(room.Data!.JoinCode!));

        harness.SignIn(host);
        await harness.Rooms.StartAsync(room.Data.Id);

        await using var context = harness.NewContext();

        var questionSetsByCompetition = await context.CompetitionQuestions
            .OrderBy(cq => cq.Order)
            .GroupBy(cq => cq.CompetitionId)
            .Select(g => g.Select(cq => cq.QuestionId).ToList())
            .ToListAsync();

        questionSetsByCompetition.Should().HaveCount(2, "her katılımcı için bir yarışma oturumu açılır");

        // Skorların karşılaştırılabilir olması için soru setleri aynı olmak zorunda.
        questionSetsByCompetition[0].Should().Equal(questionSetsByCompetition[1]);
    }

    // =====================================================================
    //  Yardımcılar
    // =====================================================================

    private static CreateRoomRequest SoloRequest(GameTestHarness harness, int questionCount) =>
        new(harness.CategoryId, null, RoomMode.Solo, questionCount, 20, 1);

    /// <summary>Sıradaki soruyu alır, saati ilerletir ve cevaplar.</summary>
    private static async Task<AnswerResultResponse> AnswerAsync(
        GameTestHarness harness,
        bool correct,
        double afterSeconds)
    {
        QuizQuestionResponse question = (await harness.Game.GetCurrentQuestionAsync()).Data
            ?? throw new InvalidOperationException("Cevaplanacak soru kalmadı.");

        harness.Clock.AdvanceSeconds(afterSeconds);

        // Harness her soruda ilk şıkkı doğru olarak kuruyor.
        Guid selectedId = correct ? question.Options[0].Id : question.Options[1].Id;

        IDataResult<AnswerResultResponse> result = await harness.Game.SubmitAnswerAsync(
            new SubmitAnswerRequest(question.CompetitionQuestionId, selectedId));

        return result.Data!;
    }
}
