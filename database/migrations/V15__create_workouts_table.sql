------------------------------------------------------------
-- WORKOUTS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE body.workouts (
    id                INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id           INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    workout_date      DATE NOT NULL,
    routine_name      VARCHAR(100) NOT NULL,
    duration_minutes  INT,
    calories_burned   NUMERIC(8,2),
    notes             VARCHAR(500),
    status            SMALLINT NOT NULL DEFAULT 1,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE body.workouts
IS 'Session-level logged training sessions (train of the day) -- one row per workout performed.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN body.workouts.id IS 'Unique identifier for each workout record (primary key).';

COMMENT ON COLUMN body.workouts.user_id
IS 'References the user who performed this workout (foreign key to users.id).';

COMMENT ON COLUMN body.workouts.workout_date IS 'Date the workout was performed.';

COMMENT ON COLUMN body.workouts.routine_name
IS 'Name of the routine performed during this session (e.g., Push Day, Leg Day).';

COMMENT ON COLUMN body.workouts.duration_minutes IS 'Total duration of the workout session, in minutes.';

COMMENT ON COLUMN body.workouts.calories_burned IS 'Estimated calories burned during the workout session.';

COMMENT ON COLUMN body.workouts.notes IS 'Optional notes about the workout session.';

COMMENT ON COLUMN body.workouts.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN body.workouts.created_at IS 'Timestamp when this workout record was created.';
