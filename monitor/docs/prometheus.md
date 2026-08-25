# Prometheus Config Documentation

## `global`

```yaml
global:
  scrape_interval: 15s  # Scrape every 15 seconds
```

Defines global settings for Prometheus behavior.

| Setting                | Meaning                                                                                                            |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `scrape_interval: 15s` | Prometheus will scrape (collect) metrics from all defined targets **every 15 seconds**, unless overridden per job. |

## `scrape_configs`

This section defines **jobs**, each tells Prometheus *what to scrape* and *where to scrape it from*.

### Job: OpenTelemetry Collector

```yaml
- job_name: 'otel-collector'
  static_configs:
    - targets: ['otel-collector:9464']
```

| Field                              | Description                                                                                                    |
| ---------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `job_name: otel-collector`         | Logical name for this scrape job (used in Prometheus UI & labels).                                             |
| `targets: ['otel-collector:9464']` | Prometheus scrapes metrics from the OTEL Collector’s Prometheus exporter endpoint, usually exposed on `:9464`. |

Provides performance metrics of the **OpenTelemetry Collector** itself (CPU, latency, queue depth, dropped telemetry, etc.).

### Job: Loki

```yaml
- job_name: 'loki'
  static_configs:
    - targets: ['loki:3100']
```

| Field                    | Description                                            |
| ------------------------ | ------------------------------------------------------ |
| `job_name: loki`         | Job name shown in Prometheus dashboards and alerts     |
| `targets: ['loki:3100']` | Scrapes from Loki's `/metrics` endpoint (on port 3100) |

Metrics include query performance, ingestion rates, request errors, chunk flush activity, etc.

### Job: postgres-exporter (PostgreSQL metrics)

```yaml
- job_name: 'postgres-exporter'
  static_configs:
    - targets: ['postgres-exporter:9187']
```

| Field                                 | Description                                                                |
| -------------------------------------- | --------------------------------------------------------------------------- |
| `job_name: postgres-exporter`         | Job name for the PostgreSQL metrics exporter                              |
| `targets: ['postgres-exporter:9187']` | Scrapes `postgres_exporter`'s `/metrics` endpoint on its default port 9187 |

`postgres_exporter` connects directly to the `fin_pulse` database (via `DATA_SOURCE_NAME`, see `monitor/docker-compose.yml`) and exposes `pg_stat_database`, `pg_stat_user_tables`, and other system-view metrics in Prometheus format, no application code changes needed.

Note: `FinPulse.Api`'s own metrics are not scraped directly by Prometheus. The API exports metrics via OTLP to the OTel Collector, which re-exposes them on the already-scraped `otel-collector:9464` target, so a second direct-scrape job for the API would be redundant.

## Overall Flow of Metrics

| Source                  | Metrics Type                                                                      | Scraped By |
| ------------------------ | ------------------------------------------------------------------------------------ | ---------- |
| OpenTelemetry Collector | Collector processing + re-exposed FinPulse.Api metrics                              | Prometheus |
| Loki                    | Log ingestion/query metrics                                                         | Prometheus |
| postgres-exporter       | PostgreSQL metrics (connections, transactions, cache hit ratio, table/index sizes)  | Prometheus |

## Usage Tips

You can visualize this in **Grafana** by adding Prometheus as a data source.

Common useful dashboards:

| Component      | Recommended Dashboard                                |
| -------------- | ---------------------------------------------------- |
| OTEL Collector | `OpenTelemetry Operations`                           |
| Loki           | `Loki Overview`                                      |
| App Metrics    | Custom (HTTP latency, requests, CPU, memory, errors) |
