------------------------------------------------------------
-- HABIT_LOGS TABLE DEFINITION
-- Daily adherence log for a habit -- one row per user per habit
-- per date, marking whether it was completed or skipped.
------------------------------------------------------------
CREATE TABLE body.habit_logs (
    id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id       INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    habit_id      INT NOT NULL REFERENCES body.habits(id),
    log_date      DATE NOT NULL,
    is_completed  BOOLEAN NOT NULL DEFAULT false,
    notes         VARCHAR(500),
    status        SMALLINT NOT NULL DEFAULT 1,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_habit_logs_user_habit_date UNIQUE (user_id, habit_id, log_date)
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE body.habit_logs
IS 'Daily adherence log for a habit -- one row per user per habit per date, marking whether it was '
'completed or skipped that day.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN body.habit_logs.id IS 'Unique identifier for each habit log entry (primary key).';

COMMENT ON COLUMN body.habit_logs.user_id
IS 'References the user who logged this entry (foreign key to users.id).';

COMMENT ON COLUMN body.habit_logs.habit_id
IS 'References the habit this log entry tracks (foreign key to body.habits.id). No cascade delete: '
'a habit with existing logs cannot be hard-deleted, only archived via its status flag.';

COMMENT ON COLUMN body.habit_logs.log_date IS 'Date this adherence entry applies to.';

COMMENT ON COLUMN body.habit_logs.is_completed IS 'Whether the habit was completed on this date (true) or skipped (false).';

COMMENT ON COLUMN body.habit_logs.notes IS 'Optional notes about this entry (e.g., reason for skipping).';

COMMENT ON COLUMN body.habit_logs.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN body.habit_logs.created_at IS 'Timestamp when this habit log entry was created.';
