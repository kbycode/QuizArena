namespace QuizArena.Core.Utilities.Results;

/// <summary>
/// Başarılı, veri taşımayan sonuç.
/// </summary>
/// <remarks>
/// Projenin ilk hâlinde bu tipin adı <c>SuccesResult</c> (tek 's') idi.
/// Yazım hatası tüm katmanlara yayılmış durumdaydı; genel API yüzeyi olduğu
/// için doğru yazıma çekildi.
/// </remarks>
public sealed class SuccessResult : Result
{
    public SuccessResult() : base(true) { }

    public SuccessResult(string message) : base(true, message) { }
}
