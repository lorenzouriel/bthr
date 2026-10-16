# Prometheus

Application exports, backend self-metrics, PostgreSQL exporter and blackbox readiness probes. The inline Prometheus config in docker-compose.yml uses Compose interpolation for target identities from .env.

See [the monitoring runbook](../README.md) for the current startup, configuration, validation and upgrade procedure.
