# Değişiklik Günlüğü

## [0.1.0] — 2026-09-06

### İlk sürüm

- Producer (Minimal API), Consumer (Worker) ve Shared kütüphanesi ile RabbitMQ Dead Letter Exchange demo çözümü eklendi.
- Topoloji: `demo.orders` / `demo.orders.retry` / `demo.orders.dlx` exchange’leri; main, TTL retry (5s) ve DLQ kuyrukları.
- Yol B retry: Transient → explicit `PublishToRetry` + publisher confirm + manual Ack; Poison / MaxRetry → DLQ + Ack (`requeue=true` yok).
- Birim testleri: `RetryDecision`, `RabbitMqOptions` doğrulama, topoloji isimleri (broker gerektirmez).
- `docker-compose.yml` ile `rabbitmq:3-management` (5672 / 15672).
- Türkçe README ve MIT lisansı.
