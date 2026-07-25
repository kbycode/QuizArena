using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Play;

/// <summary>
/// Cevap gönderme isteği.
/// </summary>
/// <remarks>
/// <b>İstekte "geçen süre" alanı bilinçli olarak yok.</b> Süre istemciden
/// alınırsa oyuncu her soru için "120 ms'de cevapladım" yazıp azami hız
/// ikramiyesini garanti eder. Sunucu, soruyu servis ettiği anı kendi
/// kaydettiği için süreyi kendisi hesaplar.
/// <para>
/// <see cref="SelectedAnswerId"/> <c>null</c> gönderilebilir: "pas geçiyorum"
/// anlamına gelir ve soru yanlış sayılır.
/// </para>
/// </remarks>
public sealed record SubmitAnswerRequest(
    Guid CompetitionQuestionId,
    Guid? SelectedAnswerId) : IDto;
