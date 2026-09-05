# Multi-stage: .NET 8 LTS only (sdk:8.0 / aspnet:8.0). Preview / 9 / 10 kullanılmaz.
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

COPY global.json ./
COPY src/RabbitMqDlx.Shared/RabbitMqDlx.Shared.csproj src/RabbitMqDlx.Shared/
COPY src/RabbitMqDlx.Producer/RabbitMqDlx.Producer.csproj src/RabbitMqDlx.Producer/
RUN dotnet restore src/RabbitMqDlx.Producer/RabbitMqDlx.Producer.csproj

COPY src/RabbitMqDlx.Shared/ src/RabbitMqDlx.Shared/
COPY src/RabbitMqDlx.Producer/ src/RabbitMqDlx.Producer/

# Örnek yapılandırma → runtime varsayılanı; compose ortam değişkenleri override eder.
RUN cp src/RabbitMqDlx.Producer/appsettings.example.json src/RabbitMqDlx.Producer/appsettings.json

RUN dotnet publish src/RabbitMqDlx.Producer/RabbitMqDlx.Producer.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" --uid 10001 appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app

USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

ENTRYPOINT ["dotnet", "RabbitMqDlx.Producer.dll"]
