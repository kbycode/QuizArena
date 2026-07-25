# =============================================================================
#  QuizArena API — çok aşamalı (multi-stage) derleme
#
#  Amaç: son imajda SDK, kaynak kod, NuGet önbelleği ve test projesi
#  bulunmasın. Çalışma zamanı imajı yalnızca derlenmiş çıktıyı taşır —
#  hem çok daha küçük hem de saldırı yüzeyi çok daha dar.
# =============================================================================

# --- 1) Derleme aşaması -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Önce yalnızca proje dosyalarını kopyala.
#
# Neden ayrı adım: Docker katmanları önbelleklenir. Kaynak kod her değiştiğinde
# değil, YALNIZCA bağımlılıklar değiştiğinde 'restore' yeniden çalışır.
# Bu, tipik bir derlemeden onlarca saniye kazandırır.
COPY QuizArena/Directory.Build.props QuizArena/
COPY QuizArena/Directory.Packages.props QuizArena/
COPY QuizArena/QuizArena.sln QuizArena/
COPY QuizArena/QuizArena.Core/*.csproj QuizArena/QuizArena.Core/
COPY QuizArena/QuizArena.Entities/*.csproj QuizArena/QuizArena.Entities/
COPY QuizArena/QuizArena.DAL/*.csproj QuizArena/QuizArena.DAL/
COPY QuizArena/QuizArena.BLL/*.csproj QuizArena/QuizArena.BLL/
COPY QuizArena/QuizArena.Api/*.csproj QuizArena/QuizArena.Api/
COPY QuizArena/QuizArena.Tests/*.csproj QuizArena/QuizArena.Tests/

RUN dotnet restore QuizArena/QuizArena.Api/QuizArena.Api.csproj

# Şimdi kaynak kodu kopyala ve yayınla.
COPY QuizArena/ QuizArena/
RUN dotnet publish QuizArena/QuizArena.Api/QuizArena.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# --- 2) Çalışma zamanı aşaması ------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Kök (root) kullanıcıyla çalıştırmıyoruz.
#
# Konteyner içindeki bir açık, kök yetkisiyle çalışıyorsa doğrudan konteyner
# kaçışı (container escape) denemelerine zemin hazırlar. .NET 8 imajları
# 'app' adında ayrıcalıksız bir kullanıcı ile birlikte gelir.
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_NOLOGO=true \
    # Türkçe kültür desteği gerekiyor (slug üretimi, tarih/sayı biçimleri).
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

COPY --from=build /app/publish .

# Konteyner sağlıklı mı? Orkestratör bu bilgiyle trafiği yönlendirir.
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD ["dotnet", "--info"]

ENTRYPOINT ["dotnet", "QuizArena.Api.dll"]
