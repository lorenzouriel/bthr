------------------------------------------------------------
-- HABITS TABLE DEFINITION
-- Habit definitions: the recurring non-workout habits a user
-- wants to track. Actual daily adherence is recorded in
-- body.habit_logs.
------------------------------------------------------------
CREATE TABLE body.habits (
    id                INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id           INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    habit_name        VARCHAR(100) NOT NULL,
    category          VARCHAR(50),
    target_frequency  VARCHAR(20) NOT NULL DEFAULT 'Daily',
    description       VARCHAR(500),
    status            SMALLINT NOT NULL DEFAULT 1,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_habits_user_name UNIQUE (user_id, habit_name)
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE body.habits
IS 'Habit definitions: the recurring non-workout habits a user wants to track (vitamins, stretching, '
'reading, no-sugar, screen-time limit, etc.). Actual daily adherence is recorded in body.habit_logs.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN body.habits.id IS 'Unique identifier for each habit definition (primary key).';

COMMENT ON COLUMN body.habits.user_id
IS 'References the user who owns this habit (foreign key to users.id).';

COMMENT ON COLUMN body.habits.habit_name
IS 'Name of the habit being tracked (e.g., Take Vitamins, Stretch, Read 10 Pages, No Sugar).';

COMMENT ON COLUMN body.habits.category
IS 'Optional free-text category label (e.g., Health, Mindfulness, Productivity).';

COMMENT ON COLUMN body.habits.target_frequency
IS 'How often the habit is meant to be performed (e.g., Daily, Weekly, Custom).';

COMMENT ON COLUMN body.habits.description IS 'Optional details about the habit.';

COMMENT ON COLUMN body.habits.status IS 'Status flag (1=Active, 0=Deleted/Archived).';

COMMENT ON COLUMN body.habits.created_at IS 'Timestamp when this habit definition was created.';
