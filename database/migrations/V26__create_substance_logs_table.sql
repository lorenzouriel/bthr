------------------------------------------------------------
-- SUBSTANCE_LOGS TABLE DEFINITION
-- Append-only log of caffeine/alcohol/nicotine intake with a
-- timestamp, so entries can be correlated against the same
-- night's body.sleep_logs.
------------------------------------------------------------
CREATE TABLE body.substance_logs (
    id              INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id         INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    consumed_at     TIMESTAMPTZ NOT NULL,
    substance_type  VARCHAR(20) NOT NULL,
    amount          NUMERIC(6,2) NOT NULL,
    unit            VARCHAR(20) NOT NULL,
    notes           VARCHAR(500),
    status          SMALLINT NOT NULL DEFAULT 1,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE body.substance_logs
IS 'Append-only log of caffeine/alcohol/nicotine intake with a timestamp, so entries can be correlated '
'against the same night''s body.sleep_logs.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN body.substance_logs.id IS 'Unique identifier for each substance log entry (primary key).';

COMMENT ON COLUMN body.substance_logs.user_id
IS 'References the user who logged this intake (foreign key to users.id).';

COMMENT ON COLUMN body.substance_logs.consumed_at IS 'Date and time the substance was consumed.';

COMMENT ON COLUMN body.substance_logs.substance_type
IS 'Type of substance consumed (e.g., Caffeine, Alcohol, Nicotine).';

COMMENT ON COLUMN body.substance_logs.amount
IS 'Quantity consumed, in the unit given by the unit column (e.g., mg of caffeine, standard drinks of alcohol).';

COMMENT ON COLUMN body.substance_logs.unit IS 'Unit of measurement for amount (e.g., mg, ml, drinks).';

COMMENT ON COLUMN body.substance_logs.notes IS 'Optional notes (e.g., drink/product name, context).';

COMMENT ON COLUMN body.substance_logs.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN body.substance_logs.created_at IS 'Timestamp when this substance log entry was created.';
