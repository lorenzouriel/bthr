------------------------------------------------------------
-- SLEEP_LOGS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE body.sleep_logs (
    id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id       INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    bed_time      TIMESTAMPTZ NOT NULL,
    wake_time     TIMESTAMPTZ NOT NULL,
    total_hours   NUMERIC(4,2) GENERATED ALWAYS AS (
        ROUND(EXTRACT(EPOCH FROM (wake_time - bed_time)) / 3600.0, 2)
    ) STORED,
    notes         VARCHAR(500),
    status        SMALLINT NOT NULL DEFAULT 1,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_sleep_logs_times CHECK (wake_time > bed_time)
);

COMMENT ON TABLE body.sleep_logs
IS 'Nightly sleep log: bed time, wake time, and total hours slept '
'(computed automatically, cannot be set directly).';

COMMENT ON COLUMN body.sleep_logs.id IS 'Unique identifier for each sleep log entry (primary key).';
COMMENT ON COLUMN body.sleep_logs.user_id IS 'References the user who owns this sleep log (foreign key to users.id).';
COMMENT ON COLUMN body.sleep_logs.bed_time IS 'Date and time the user went to bed.';
COMMENT ON COLUMN body.sleep_logs.wake_time
IS 'Date and time the user woke up. Must be after bed_time (enforced by check constraint), '
'correctly handles sessions that cross midnight since both are full timestamps.';
COMMENT ON COLUMN body.sleep_logs.total_hours
IS 'Total hours slept, automatically computed from wake_time minus bed_time. '
'Cannot be written directly (generated column).';
COMMENT ON COLUMN body.sleep_logs.notes IS 'Optional notes about sleep quality or disturbances.';
COMMENT ON COLUMN body.sleep_logs.status IS 'Status flag (1=Active, 0=Deleted).';
COMMENT ON COLUMN body.sleep_logs.created_at IS 'Timestamp when this sleep log entry was created.';
