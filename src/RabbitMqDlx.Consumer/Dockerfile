# Multi-stage: .NET 8 LTS only (sdk:8.0 / runtime:8.0). Preview / 9 / 10 kullanılmaz.
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

COPY global.json ./
COPY src/RabbitMqDlx.Shared/RabbitMqDlx.Shared.csproj src/RabbitMqDlx.Shared/
COPY src/RabbitMqDlx.Consumer/RabbitMqDlx.Consumer.csproj src/RabbitMqDlx.Consumer/
RUN dotnet restore src/RabbitMqDlx.Consumer/RabbitMqDlx.Consumer.csproj

COPY src/RabbitMqDlx.Shared/ src/RabbitMqDlx.Shared/
COPY src/RabbitMqDlx.Consumer/ src/RabbitMqDlx.Consumer/

# Örnek yapılandırma → runtime varsayılanı; compose ortam değişkenleri override eder.
RUN cp src/RabbitMqDlx.Consumer/appsettings.example.json src/RabbitMqDlx.Consumer/appsettings.json

RUN dotnet publish src/RabbitMqDlx.Consumer/RabbitMqDlx.Consumer.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" --uid 10001 appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app

USER appuser

ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "RabbitMqDlx.Consumer.dll"]
