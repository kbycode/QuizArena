using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace QuizArena.Core.Aspects.Logging;

/// <summary>
/// Metot çağrısını, argümanlarıyla birlikte loglar.
/// </summary>
/// <remarks>
/// <para>
/// Kullanım: <c>[LogAspect]</c>
/// </para>
/// <para>
/// <b>En kritik ayrıntı: hassas veri maskeleme.</b> Bir kimlik doğrulama
/// metodunu düşünmeden loglamak, <c>LoginRequest</c> nesnesinin
/// <b>parolayı düz metin hâlinde log dosyasına yazmasına</b> yol açar. Bu,
/// KVKK/GDPR açısından ihlal, güvenlik açısından ise "veritabanını değil
/// logları çal" saldırısına açık kapıdır. Aşağıdaki maskeleme, adında
/// <c>password</c>, <c>token</c>, <c>secret</c>, <c>hash</c> gibi ifade
/// bulunan her alanı loglamadan önce yıldızlar.
/// </para>
/// </remarks>
public sealed class LogAspect : AspectAttribute
{
    private const string MaskedValue = "***";
    private const int MaxSerializedLength = 2_000;

    /// <summary>Maskeleme yapılacak alan adı parçaları (küçük harf, karşılaştırma "içerir").</summary>
    private static readonly string[] SensitiveNameFragments =
    [
        "password", "parola", "sifre", "şifre",
        "token", "secret", "apikey", "authorization",
        "hash", "salt", "securitystamp", "creditcard"
    ];

    public LogAspect() => Order = 15;

    public LogLevel Level { get; init; } = LogLevel.Information;

    public override void OnBefore(AspectContext context)
    {
        ILogger logger = CreateLogger(context);

        if (!logger.IsEnabled(Level))
        {
            return;
        }

        logger.Log(
            Level,
            "→ {Type}.{Method} çağrıldı. Argümanlar: {Arguments}",
            context.Method.DeclaringType?.Name,
            context.Method.Name,
            DescribeArguments(context));
    }

    public override ValueTask OnExceptionAsync(AspectContext context, Exception exception)
    {
        CreateLogger(context).LogError(
            exception,
            "✕ {Type}.{Method} hata ile sonuçlandı. Argümanlar: {Arguments}",
            context.Method.DeclaringType?.Name,
            context.Method.Name,
            DescribeArguments(context));

        return ValueTask.CompletedTask;
    }

    private static ILogger CreateLogger(AspectContext context)
        => context.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(context.Method.DeclaringType?.FullName ?? "QuizArena.Aspect");

    private static string DescribeArguments(AspectContext context)
    {
        var parameters = context.Method.GetParameters();
        var parts = new List<string>(context.Arguments.Length);

        for (int i = 0; i < context.Arguments.Length; i++)
        {
            string name = i < parameters.Length ? parameters[i].Name ?? $"arg{i}" : $"arg{i}";
            parts.Add($"{name}={Describe(name, context.Arguments[i])}");
        }

        return string.Join(", ", parts);
    }

    private static string Describe(string parameterName, object? value)
    {
        if (value is null)
        {
            return "null";
        }

        // 1) Parametrenin kendi adı hassas mı? (ör. Login(string password))
        if (IsSensitiveName(parameterName))
        {
            return MaskedValue;
        }

        // 2) İptal jetonu gibi anlamı olmayan tipleri loglamaya değmez.
        if (value is CancellationToken)
        {
            return "<ct>";
        }

        // 3) İlkel tipler doğrudan yazılabilir.
        if (value is string or bool or Guid || value.GetType().IsPrimitive
            || value is DateTime or DateTimeOffset or DateOnly or decimal or Enum)
        {
            return value.ToString() ?? "null";
        }

        // 4) Karmaşık nesneler: hassas özellikleri maskelenerek serileştirilir.
        return SerializeMasked(value);
    }

    private static string SerializeMasked(object value)
    {
        try
        {
            var masked = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var property in value.GetType().GetProperties())
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                masked[property.Name] = IsSensitiveName(property.Name)
                    ? MaskedValue
                    : SafeReadValue(property, value);
            }

            string json = JsonSerializer.Serialize(masked);

            return json.Length <= MaxSerializedLength
                ? json
                : string.Concat(json.AsSpan(0, MaxSerializedLength), "…(kısaltıldı)");
        }
        catch (Exception)
        {
            // Loglama hiçbir koşulda iş akışını bozmamalı.
            return $"<{value.GetType().Name}>";
        }
    }

    private static object? SafeReadValue(System.Reflection.PropertyInfo property, object instance)
    {
        try
        {
            object? raw = property.GetValue(instance);

            // Koleksiyonları/nav özelliklerini açmaya çalışmak tembel yükleme
            // tetikleyebilir; yalnızca basit değerleri yazıyoruz.
            return raw switch
            {
                null => null,
                string or bool or Guid or DateTime or DateTimeOffset or DateOnly or decimal or Enum => raw,
                _ when raw.GetType().IsPrimitive => raw,
                _ => $"<{raw.GetType().Name}>"
            };
        }
        catch (Exception)
        {
            return "<okunamadı>";
        }
    }

    private static bool IsSensitiveName(string name)
        => Array.Exists(
            SensitiveNameFragments,
            fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
