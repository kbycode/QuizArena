using System.Reflection;

namespace QuizArena.Core.Aspects;

/// <summary>
/// Bir aspect'in çalışması için gereken her şeyi taşıyan bağlam nesnesi.
/// Metot çağrısı başına bir kez üretilir ve tüm aspect'ler arasında paylaşılır.
/// </summary>
public sealed class AspectContext
{
    internal AspectContext(IServiceProvider services, MethodInfo method, object?[] arguments, Type returnType)
    {
        Services = services;
        Method = method;
        Arguments = arguments;
        ReturnType = returnType;
    }

    /// <summary>
    /// İstek kapsamındaki (scoped) servis sağlayıcı.
    /// <para>
    /// Aspect'ler <see cref="Attribute"/> oldukları için kurucu enjeksiyonu
    /// alamazlar — .NET, attribute örneklerini bizim yerimize üretir. Klasik
    /// çözüm statik bir <c>ServiceTool.Resolve&lt;T&gt;()</c> yerel hizmet
    /// bulucusudur; ancak o, gizli global durum yaratır ve testi zorlaştırır.
    /// Burada sağlayıcı, aspect'i çalıştıran <see cref="AspectInterceptor"/>
    /// tarafından bağlama <b>enjekte edilir</b>; global statik yoktur ve
    /// testte sahte bir sağlayıcı verilebilir.
    /// </para>
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>Çağrılan metodun yansıma (reflection) bilgisi.</summary>
    public MethodInfo Method { get; }

    /// <summary>Metoda geçilen argümanlar.</summary>
    public object?[] Arguments { get; }

    /// <summary>
    /// Metodun mantıksal dönüş tipi. <c>Task&lt;T&gt;</c> için <c>T</c>,
    /// <c>Task</c> için <see cref="void"/>.
    /// </summary>
    public Type ReturnType { get; }

    /// <summary>
    /// Metodun (veya kısa devre yapan aspect'in) dönüş değeri.
    /// <c>OnSuccessAsync</c> içinde okunabilir ve değiştirilebilir.
    /// </summary>
    public object? ReturnValue { get; set; }

    /// <summary>
    /// <c>true</c> ise hedef metot <b>hiç çağrılmaz</b> ve
    /// <see cref="ReturnValue"/> doğrudan döner. Önbellek isabetinin
    /// (cache hit) çalışma biçimi budur.
    /// </summary>
    public bool ShortCircuited { get; private set; }

    /// <summary>Aspect'lerin <c>OnBefore</c> ile <c>OnSuccess</c> arasında veri taşıdığı torba.</summary>
    public Dictionary<string, object?> State { get; } = [];

    public void ShortCircuit(object? returnValue)
    {
        ReturnValue = returnValue;
        ShortCircuited = true;
    }
}
