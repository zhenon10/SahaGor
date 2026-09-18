# syntax=docker/dockerfile:1
#
# SahaGor.Api production imaji (SG-422). Build context REPO KOKU olmalidir (docker-compose.prod.yml
# bunu "context: ." + "dockerfile: src/SahaGor.Api/Dockerfile" ile saglar), cunku Api projesi
# Domain/Application/Infrastructure projelerine referans verir.
#
# Alpine yerine bilerek standart (glibc tabanli) Debian imajlari kullanilir: Npgsql/EF Core ve
# ICU/globalization (kultur-duyarli metin islemleri) Alpine/musl'da gecmiste sorun cikarmis;
# production'da "calisiyor mu diye ugrasmak" yerine daha az surpriz iceren imaji tercih ederiz.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Once sadece .csproj dosyalari kopyalanip restore edilir; boylece sadece kaynak kod (.cs)
# degistiginde NuGet paketlerinin tamami yeniden indirilmez (Docker katman onbellegi).
COPY src/SahaGor.Domain/SahaGor.Domain.csproj src/SahaGor.Domain/
COPY src/SahaGor.Application/SahaGor.Application.csproj src/SahaGor.Application/
COPY src/SahaGor.Infrastructure/SahaGor.Infrastructure.csproj src/SahaGor.Infrastructure/
COPY src/SahaGor.Api/SahaGor.Api.csproj src/SahaGor.Api/
RUN dotnet restore src/SahaGor.Api/SahaGor.Api.csproj

COPY src/ src/
RUN dotnet publish src/SahaGor.Api/SahaGor.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Konteyner ele gecirilse dahi host'a/sisteme erisimi sinirlamak icin root olmayan kullanici.
RUN groupadd --system sahagor && useradd --system --gid sahagor --home /app sahagor \
    && mkdir -p /app/App_Data/gorev-fotograflari /app/logs \
    && chown -R sahagor:sahagor /app

COPY --from=build --chown=sahagor:sahagor /app .

USER sahagor
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SahaGor.Api.dll"]
