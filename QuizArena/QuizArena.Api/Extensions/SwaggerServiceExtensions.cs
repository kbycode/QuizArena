using System.Reflection;
using Microsoft.OpenApi.Models;

namespace QuizArena.Api.Extensions;

/// <summary>Swagger/OpenAPI yapılandırması.</summary>
public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "QuizArena API",
                Version = "v1",
                Description = """
                    Çok oyunculu bilgi yarışması API'si.

                    **Kimlik doğrulama:** `POST /api/auth/register` veya `POST /api/auth/login`
                    ile aldığınız `accessToken` değerini sağ üstteki **Authorize** düğmesine
                    yapıştırın (yalnızca jetonu; `Bearer` öneki otomatik eklenir).

                    **Oyun akışı:** oda kur → (çok oyunculu ise) katılım kodunu paylaş →
                    başlat → `GET /api/play/current` ile soruyu al → `POST /api/play/answer`
                    ile cevapla → `GET /api/play/summary/{competitionId}` ile sonucu gör.
                    """
            });

            // --- JWT desteği ------------------------------------------------
            // Bu tanım olmadan Swagger arayüzünden korumalı uçlar denenemez;
            // her istek 401 döner ve API "bozuk" görünür.
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "JWT erişim jetonunuzu yapıştırın ('Bearer ' öneki gerekmez).",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [securityScheme] = []
            });

            // XML yorumları uç açıklamalarına dönüşür.
            string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            // Enum'lar sayı değil isim olarak görünsün: "difficulty: 3" yerine
            // "difficulty: Hard" belgeyi kendi kendini açıklayan hâle getirir.
            options.UseInlineDefinitionsForEnums();
        });

        return services;
    }
}
