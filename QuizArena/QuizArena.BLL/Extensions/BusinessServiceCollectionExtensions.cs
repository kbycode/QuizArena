using System.Reflection;
using QuizArena.BLL.Notifications;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace QuizArena.BLL.Extensions;

/// <summary>İş katmanının DI kayıtları.</summary>
public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Doğrulayıcılar taranarak kaydedilir. Yeni bir validator eklendiğinde
        // burada bir satır yazmak gerekmez — unutulma riski ortadan kalkar.
        services.AddValidatorsFromAssembly(
            Assembly.GetExecutingAssembly(),
            lifetime: ServiceLifetime.Singleton,
            includeInternalTypes: false);

        // Bildirim altyapısı: API katmanı SignalR uygulamasını kaydetmezse
        // hiçbir şey yapmayan sürüm devreye girer ve oyun akışı bozulmaz.
        // TryAdd kullanılıyor ki gerçek uygulama zaten kayıtlıysa ezilmesin.
        services.TryAddSingleton<IGameNotifier, NullGameNotifier>();

        // İş servislerinin kendisi Autofac modülünde kaydediliyor
        // (bkz. AutofacBusinessModule) — aspect'lerin çalışması için
        // arayüz kesme (interface interception) gerekiyor.
        return services;
    }
}
