------------------------------------------------------------
-- SYMPTOM_LOGS TABLE DEFINITION
-- Append-only log of symptoms/illness, one row per symptom
-- reported per date, explaining days that fell off the normal
-- routine.
------------------------------------------------------------
CREATE TABLE body.symptom_logs (
    id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id       INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    log_date      DATE NOT NULL,
    symptom       VARCHAR(100) NOT NULL,
    severity      SMALLINT,
    notes         VARCHAR(500),
    status        SMALLINT NOT NULL DEFAULT 1,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT ck_symptom_logs_severity CHECK (severity IS NULL OR severity BETWEEN 1 AND 5)
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE body.symptom_logs
IS 'Append-only log of symptoms/illness, one row per symptom reported per date, explaining days that '
'fell off the normal routine.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN body.symptom_logs.id IS 'Unique identifier for each symptom log entry (primary key).';

COMMENT ON COLUMN body.symptom_logs.user_id
IS 'References the user who logged this symptom (foreign key to users.id).';

COMMENT ON COLUMN body.symptom_logs.log_date IS 'Date the symptom was experienced.';

COMMENT ON COLUMN body.symptom_logs.symptom
IS 'Name of the symptom (e.g., Headache, Fever, Sore Throat, Fatigue).';

COMMENT ON COLUMN body.symptom_logs.severity IS 'Optional self-reported severity, on a 1-5 scale.';

COMMENT ON COLUMN body.symptom_logs.notes IS 'Optional free-text notes about the symptom or its context.';

COMMENT ON COLUMN body.symptom_logs.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN body.symptom_logs.created_at IS 'Timestamp when this symptom log entry was created.';
