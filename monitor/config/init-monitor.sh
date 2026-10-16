#!/bin/sh
set -eu
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set=monitor_password="$MONITOR_DB_PASSWORD" <<'SQL'
CREATE ROLE bthr_monitor LOGIN PASSWORD :'monitor_password';
GRANT pg_monitor TO bthr_monitor;
GRANT CONNECT ON DATABASE bthr TO bthr_monitor;
SQL
