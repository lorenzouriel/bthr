------------------------------------------------------------
-- WATER_INTAKE TABLE DEFINITION
-- Daily water intake as a running total -- one row per user per
-- day, incremented as water is logged throughout the day.
------------------------------------------------------------
CREATE TABLE body.water_intake (
    id           INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id      INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    intake_date  DATE NOT NULL,
    amount_ml    INT NOT NULL DEFAULT 0,
    status       SMALLINT NOT NULL DEFAULT 1,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_water_intake_user_date UNIQUE (user_id, intake_date)
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE body.water_intake
IS 'Daily water intake as a running total -- one row per user per day, incremented as water is '
'logged throughout the day.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN body.water_intake.id IS 'Unique identifier for each water intake record (primary key).';

COMMENT ON COLUMN body.water_intake.user_id
IS 'References the user who logged this water intake (foreign key to users.id).';

COMMENT ON COLUMN body.water_intake.intake_date IS 'Date the water intake total applies to.';

COMMENT ON COLUMN body.water_intake.amount_ml
IS 'Running total amount of water consumed on this date, in milliliters.';

COMMENT ON COLUMN body.water_intake.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN body.water_intake.created_at IS 'Timestamp when this water intake record was created.';
