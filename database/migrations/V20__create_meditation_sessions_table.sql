------------------------------------------------------------
-- MEDITATION SESSIONS TABLE DEFINITION
-- Per-session meditation log with optional before/after mood.
------------------------------------------------------------
CREATE TABLE mind.meditation_sessions (
    id                 INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id            INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    session_date       DATE NOT NULL,
    duration_minutes   SMALLINT NOT NULL CHECK (duration_minutes > 0),
    meditation_type    VARCHAR(50) NOT NULL,
    mood_before        SMALLINT CHECK (mood_before IS NULL OR mood_before BETWEEN 1 AND 5),
    mood_after         SMALLINT CHECK (mood_after IS NULL OR mood_after BETWEEN 1 AND 5),
    notes              VARCHAR(500),
    status             SMALLINT NOT NULL DEFAULT 1,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE mind.meditation_sessions
IS 'Per-session meditation log with optional before/after mood ratings.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN mind.meditation_sessions.id IS 'Unique identifier for each meditation session record (primary key).';

COMMENT ON COLUMN mind.meditation_sessions.user_id IS 'References the user who logged this session (foreign key to users.id).';

COMMENT ON COLUMN mind.meditation_sessions.session_date IS 'Date the meditation session took place.';

COMMENT ON COLUMN mind.meditation_sessions.duration_minutes IS 'Length of the session in minutes; must be greater than zero.';

COMMENT ON COLUMN mind.meditation_sessions.meditation_type
IS 'Type of meditation practiced (e.g., Guided, Breathing, Body Scan).';

COMMENT ON COLUMN mind.meditation_sessions.mood_before IS 'Optional self-reported mood before the session, on a 1-5 scale.';

COMMENT ON COLUMN mind.meditation_sessions.mood_after IS 'Optional self-reported mood after the session, on a 1-5 scale.';

COMMENT ON COLUMN mind.meditation_sessions.notes IS 'Optional free-text notes about the session.';

COMMENT ON COLUMN mind.meditation_sessions.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN mind.meditation_sessions.created_at IS 'Timestamp when this session record was created.';
