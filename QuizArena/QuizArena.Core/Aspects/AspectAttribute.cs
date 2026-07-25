namespace QuizArena.Core.Aspects;

/// <summary>
/// Tüm aspect'lerin taban sınıfı. Bir metodun üzerine yazılan
/// <c>[ValidationAspect(...)]</c>, <c>[CacheAspect]</c> gibi nitelikler
/// bundan türer.
/// </summary>
/// <remarks>
/// <para>
/// <b>Aspect nedir, neye çözüm?</b> Doğrulama, önbellek, loglama, transaction
/// ve yetki kontrolü her servis metodunda tekrar eden ama <b>işin kendisi
/// olmayan</b> davranışlardır (cross-cutting concerns). Elle yazıldığında
/// 5 satırlık iş kuralı 40 satırlık altyapı gürültüsü içinde kaybolur.
/// Aspect'ler bu davranışları metodun üstüne bir nitelik olarak taşır;
/// iş metodu yalnızca iş kuralını içerir.
/// </para>
/// <para>
/// <b>Yaşam döngüsü:</b>
/// <c>OnBefore</c> (tümü, <see cref="Order"/> sırasıyla) → hedef metot →
/// başarılıysa <c>OnSuccessAsync</c>, hata varsa <c>OnExceptionAsync</c>
/// (ikisi de <b>ters</b> sırada) → <c>OnFinallyAsync</c> (ters sırada).
/// Ters sıra, iç içe geçmiş kaynakların (transaction, kilit) doğru sırayla
/// kapatılmasını garanti eder.
/// </para>
/// <para>
/// <b><c>OnBefore</c> neden senkron?</b> Castle DynamicProxy'de hedef metoda
/// geçiş (<c>Proceed()</c>), kesici (interceptor) metodundan çıkılmadan
/// yapılmalıdır. <c>OnBefore</c> içinde <c>await</c> olsaydı <c>Proceed()</c>
/// çağrısı başka bir iş parçacığına düşebilirdi. Hedef metot beklendikten
/// <b>sonra</b> çalışan kancalar ise güvenle asenkron olabilir.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public abstract class AspectAttribute : Attribute
{
    /// <summary>
    /// Çalışma sırası; küçük olan önce çalışır.
    /// Örnek: doğrulama (10) transaction'dan (30) önce çalışmalıdır — geçersiz
    /// veri için boşuna transaction açmanın anlamı yok.
    /// </summary>
    public int Order { get; init; }

    /// <summary>Hedef metottan önce. Kısa devre için <see cref="AspectContext.ShortCircuit"/> çağrılır.</summary>
    public virtual void OnBefore(AspectContext context) { }

    /// <summary>Hedef metot başarıyla tamamlandıktan sonra.</summary>
    public virtual ValueTask OnSuccessAsync(AspectContext context) => ValueTask.CompletedTask;

    /// <summary>Hedef metot istisna attıysa. İstisna burada yutulmaz, yeniden fırlatılır.</summary>
    public virtual ValueTask OnExceptionAsync(AspectContext context, Exception exception) => ValueTask.CompletedTask;

    /// <summary>Sonuç ne olursa olsun çalışır (kaynak bırakma).</summary>
    public virtual ValueTask OnFinallyAsync(AspectContext context) => ValueTask.CompletedTask;
}
