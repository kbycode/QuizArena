using QuizArena.Core.Entities.Concrete;
using QuizArena.Core.Utilities.Results;
using QuizArena.Entities.Concrete;
using QuizArena.Entities.Dtos.Play;
using QuizArena.Entities.Dtos.Questions;
using QuizArena.Entities.Dtos.Rooms;
using QuizArena.Entities.Enums;
using QuizArena.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace QuizArena.Tests.Game;

/// <summary>
/// Soru düzenlemenin geçmiş yarışmalarla etkileşimi.
/// </summary>
/// <remarks>
/// Buradaki senaryo yalnızca <b>gerçek veriyle</b> ortaya çıkıyor: soru
/// hiç oynanmamışsa düzenleme sorunsuz çalışır. Bir kez cevaplandıktan
/// sonra ise şık kimlikleri <c>CompetitionAnswers</c> tarafından referans
/// alınır ve yabancı anahtar kısıtı devreye girer.
/// </remarks>
public sealed class QuestionEditingTests
{
    [Fact]
    public async Task Oynanmis_bir_soru_duzenlenebilir()
    {
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User player = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(player);

        // Bir tur oyna: şıklardan biri CompetitionAnswers'ta referans olsun.
        await harness.Rooms.CreateAsync(new CreateRoomRequest(
            harness.CategoryId, "Tek kişilik", RoomMode.Solo, 5, 20, 1));

        QuizQuestionResponse first = (await harness.Game.GetCurrentQuestionAsync()).Data!;
        Guid answeredQuestionOptionId = first.Options[0].Id;

        await harness.Game.SubmitAnswerAsync(
            new SubmitAnswerRequest(first.CompetitionQuestionId, answeredQuestionOptionId));

        // Cevaplanan sorunun kimliğini bul.
        Guid questionId;
        await using (var context = harness.NewContext())
        {
            questionId = await context.Answers
                .Where(a => a.Id == answeredQuestionOptionId)
                .Select(a => a.QuestionId)
                .SingleAsync();
        }

        QuestionResponse before = (await harness.Questions.GetByIdAsync(questionId)).Data!;

        // Şimdi o soruyu düzenle. Yabancı anahtar kısıtı yüzünden bu işlem
        // eskiden 500 üretiyordu.
        IDataResult<QuestionResponse> updated = await harness.Questions.UpdateAsync(
            questionId,
            new UpdateQuestionRequest(
                before.CategoryId,
                "Düzenlenmiş soru metni: hangisi doğru şıktır?",
                QuestionDifficulty.Hard,
                25,
                "Güncellenmiş açıklama.",
                IsActive: true,
                Answers:
                [
                    new SaveAnswerRequest("Yeni şık A", true, 1),
                    new SaveAnswerRequest("Yeni şık B", false, 2),
                    new SaveAnswerRequest("Yeni şık C", false, 3)
                ]));

        updated.Success.Should().BeTrue();
        updated.Data!.Text.Should().StartWith("Düzenlenmiş");
        updated.Data.Answers.Should().HaveCount(3);
    }

    [Fact]
    public async Task Duzenleme_gecmis_cevabin_kaydini_bozmaz()
    {
        // Eski şıklar yumuşak siliniyor: satır duruyor, referans geçerli
        // kalıyor, yalnızca yeni sorgularda görünmüyor. Geçmiş yarışmanın
        // hangi şıkkı seçtiği bilgisi böylece korunuyor.
        await using GameTestHarness harness = await GameTestHarness.CreateAsync(questionCount: 5);
        User player = await harness.CreateUserAsync("Oyuncu1");
        harness.SignIn(player);

        await harness.Rooms.CreateAsync(new CreateRoomRequest(
            harness.CategoryId, "Tek kişilik", RoomMode.Solo, 5, 20, 1));

        QuizQuestionResponse first = (await harness.Game.GetCurrentQuestionAsync()).Data!;
        Guid selectedAnswerId = first.Options[0].Id;

        await harness.Game.SubmitAnswerAsync(
            new SubmitAnswerRequest(first.CompetitionQuestionId, selectedAnswerId));

        Guid questionId;
        await using (var context = harness.NewContext())
        {
            questionId = await context.Answers
                .Where(a => a.Id == selectedAnswerId)
                .Select(a => a.QuestionId)
                .SingleAsync();
        }

        QuestionResponse before = (await harness.Questions.GetByIdAsync(questionId)).Data!;

        await harness.Questions.UpdateAsync(
            questionId,
            new UpdateQuestionRequest(
                before.CategoryId,
                "Tamamen değiştirilmiş soru metni burada.",
                before.Difficulty,
                before.TimeLimitSeconds,
                null,
                IsActive: true,
                Answers:
                [
                    new SaveAnswerRequest("A", true, 1),
                    new SaveAnswerRequest("B", false, 2)
                ]));

        await using var verify = harness.NewContext();

        // Seçilen şık hâlâ var (yumuşak silinmiş) ve cevap kaydı ona bakıyor.
        Answer old = await verify.Answers
            .IgnoreQueryFilters()
            .SingleAsync(a => a.Id == selectedAnswerId);

        old.IsDeleted.Should().BeTrue();

        bool referenceIntact = await verify.CompetitionAnswers
            .AnyAsync(ca => ca.SelectedAnswerId == selectedAnswerId);

        referenceIntact.Should().BeTrue();

        // Yeni şıklar normal sorguda görünür; eskiler görünmez.
        List<Answer> visible = await verify.Answers
            .Where(a => a.QuestionId == questionId)
            .ToListAsync();

        visible.Should().HaveCount(2);
        visible.Should().OnlyContain(a => !a.IsDeleted);
    }
}
