# Değişiklik Günlüğü

## [0.1.6] — 2026-09-06

### Güvenlik

- `docker-compose.yml`: RabbitMQ (`5672`, `15672`) ve Producer portları `127.0.0.1` ile bind edildi (demo credential’ların LAN’a açılması riski azaltıldı).
- README: localhost bind notu eklendi.

## [0.1.5] — 2026-09-06

### Düzeltmeler / dokümantasyon

- Solution: `RabbitMqDlx.Tests` yanlışlıkla `src` solution folder altında nested’di; `tests` solution folder’a taşındı (disk yolu `tests/` zaten doğruydu).
- README: smoke senaryoları tablosu ve Management UI / beklenen log-kuyruk çıktıları netleştirildi.

## [0.1.4] — 2026-09-06

### CI

- `.github/workflows/ci.yml` eklendi: `main` için push/PR; `setup-dotnet` **8.0.x**; solution üzerinde restore → build → test.
- README’ye kısa CI notu eklendi.

## [0.1.3] — 2026-09-06

### Docker / DevOps

- Producer ve Consumer için multi-stage Dockerfile eklendi (`mcr.microsoft.com/dotnet/sdk:8.0` / `aspnet|runtime:8.0`; preview/9/10 yok).
- `docker-compose.yml`: RabbitMQ Management korundu; isteğe bağlı `producer` / `consumer` servisleri (`profile: apps`, `depends_on` + healthy).
- Demo için `docker/rabbitmq/rabbitmq.conf` (`loopback_users.guest = false`) ve `.env.example` eklendi; `.env` gitignore’da kalır.
- README Docker bölümü güncellendi (`d:\SoftWare\dotnet-rabbitmq-dead-letter-exchange`).

## [0.1.2] — 2026-09-06

### Değişiklik

- Hedef çerçeve `.NET 8 LTS` / `net8.0` olarak standartlaştırıldı.
- Microsoft.Extensions.* paketleri 8.0.x stabil sürümlere çekildi.
- `global.json` ile SDK 8 roll-forward eklendi.

## [0.1.1] — 2026-09-06

### Düzeltmeler

- Producer `POST /api/messages`: `Amount` için `Range(typeof(decimal), "0.01", …)` kaldırıldı; tr-TR kültüründe validation 500 üretiyordu. Yerine kültür bağımsız `[Range(0.01, 999999999)]` (double overload) kullanıldı.
- `.gitignore`: `**/appsettings.*.json` eklendi (`appsettings.Development.json` / Production kapsanır); `appsettings.example.json` tracked kalır.

### Testler

- `PublishMessageRequestValidationTests`: tr-TR kültüründe geçerli Amount kabulü ve sıfır/negatif red.

## [0.1.0] — 2026-09-06

### İlk sürüm

- Producer (Minimal API), Consumer (Worker) ve Shared kütüphanesi ile RabbitMQ Dead Letter Exchange demo çözümü eklendi.
- Topoloji: `demo.orders` / `demo.orders.retry` / `demo.orders.dlx` exchange’leri; main, TTL retry (5s) ve DLQ kuyrukları.
- Yol B retry: Transient → explicit `PublishToRetry` + publisher confirm + manual Ack; Poison / MaxRetry → DLQ + Ack (`requeue=true` yok).
- Birim testleri: `RetryDecision`, `RabbitMqOptions` doğrulama, topoloji isimleri (broker gerektirmez).
- `docker-compose.yml` ile `rabbitmq:3-management` (5672 / 15672).
- Türkçe README ve MIT lisansı.
