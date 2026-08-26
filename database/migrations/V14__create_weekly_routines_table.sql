------------------------------------------------------------
-- WEEKLY_ROUTINES TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE body.weekly_routines (
    id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id       INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    day_of_week   SMALLINT NOT NULL,
    routine_name  VARCHAR(100) NOT NULL,
    description   VARCHAR(500),
    status        SMALLINT NOT NULL DEFAULT 1,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT ck_weekly_routines_day_of_week CHECK (day_of_week BETWEEN 0 AND 6),
    CONSTRAINT uq_weekly_routines_user_day UNIQUE (user_id, day_of_week)
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE body.weekly_routines
IS 'Reusable weekly training plan template: what is normally planned for each day of the week, '
'one row per user per day.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN body.weekly_routines.id IS 'Unique identifier for each weekly routine entry (primary key).';

COMMENT ON COLUMN body.weekly_routines.user_id
IS 'References the user who owns this routine (foreign key to users.id).';

COMMENT ON COLUMN body.weekly_routines.day_of_week
IS 'Day of week this routine applies to: 0=Sunday, 1=Monday, ..., 6=Saturday (matches PostgreSQL '
'EXTRACT(DOW) convention).';

COMMENT ON COLUMN body.weekly_routines.routine_name
IS 'Name of the planned routine for this day (e.g., Push Day, Rest Day, Leg Day).';

COMMENT ON COLUMN body.weekly_routines.description IS 'Optional details about what this routine involves.';

COMMENT ON COLUMN body.weekly_routines.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN body.weekly_routines.created_at IS 'Timestamp when this routine entry was created.';
