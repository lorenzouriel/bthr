------------------------------------------------------------
-- PERSONAL_RECORDS TABLE DEFINITION
-- Append-only history: a new row is inserted each time a record
-- is broken for a given exercise/metric_type, not updated in place.
------------------------------------------------------------
CREATE TABLE body.personal_records (
    id              INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id         INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    exercise_name   VARCHAR(100) NOT NULL,
    metric_type     VARCHAR(50) NOT NULL,
    value           NUMERIC(10,2) NOT NULL,
    unit            VARCHAR(20) NOT NULL,
    achieved_date   DATE NOT NULL,
    notes           VARCHAR(500),
    status          SMALLINT NOT NULL DEFAULT 1,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE body.personal_records
IS 'Append-only history of personal records -- a new row is inserted each time a record is broken '
'for a given exercise/metric_type, not updated in place.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN body.personal_records.id IS 'Unique identifier for each personal record entry (primary key).';

COMMENT ON COLUMN body.personal_records.user_id
IS 'References the user who achieved this record (foreign key to users.id).';

COMMENT ON COLUMN body.personal_records.exercise_name
IS 'Name of the exercise this record applies to (e.g., Bench Press, Deadlift, 5k Run).';

COMMENT ON COLUMN body.personal_records.metric_type
IS 'Type of metric being recorded (e.g., Max Weight, Max Reps, Best Time).';

COMMENT ON COLUMN body.personal_records.value IS 'Numeric value achieved for this record.';

COMMENT ON COLUMN body.personal_records.unit IS 'Unit of measurement for the value (e.g., kg, reps, seconds).';

COMMENT ON COLUMN body.personal_records.achieved_date IS 'Date the record was achieved.';

COMMENT ON COLUMN body.personal_records.notes IS 'Optional notes about the conditions under which the record was set.';

COMMENT ON COLUMN body.personal_records.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN body.personal_records.created_at IS 'Timestamp when this personal record entry was created.';
