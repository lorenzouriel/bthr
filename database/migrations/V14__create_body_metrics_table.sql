------------------------------------------------------------
-- BODY_METRICS TABLE DEFINITION
-- History of body measurements over time -- one row per user per
-- date logged, enabling trend tracking.
------------------------------------------------------------
CREATE TABLE body.body_metrics (
    id                 INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id            INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    measured_date      DATE NOT NULL,
    weight_kg          NUMERIC(5,2),
    height_cm          NUMERIC(5,2),
    body_fat_percent   NUMERIC(4,2),
    notes              VARCHAR(500),
    status             SMALLINT NOT NULL DEFAULT 1,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_body_metrics_user_date UNIQUE (user_id, measured_date)
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE body.body_metrics
IS 'History of body measurements (weight, height, body fat) over time -- one row per user per date '
'logged, enabling trend tracking.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN body.body_metrics.id IS 'Unique identifier for each body metrics record (primary key).';

COMMENT ON COLUMN body.body_metrics.user_id
IS 'References the user this measurement belongs to (foreign key to users.id).';

COMMENT ON COLUMN body.body_metrics.measured_date IS 'Date the measurements were taken.';

COMMENT ON COLUMN body.body_metrics.weight_kg IS 'Body weight measured in kilograms.';

COMMENT ON COLUMN body.body_metrics.height_cm IS 'Height measured in centimeters.';

COMMENT ON COLUMN body.body_metrics.body_fat_percent IS 'Body fat percentage.';

COMMENT ON COLUMN body.body_metrics.notes IS 'Optional notes about this measurement (e.g., method used).';

COMMENT ON COLUMN body.body_metrics.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN body.body_metrics.created_at IS 'Timestamp when this body metrics record was created.';
