------------------------------------------------------------
-- JOURNAL ENTRIES TABLE DEFINITION
-- Free-form journal entries with optional mood and category.
------------------------------------------------------------
CREATE TABLE mind.journal_entries (
    id             INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id        INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    entry_date     DATE NOT NULL,
    title          VARCHAR(200),
    content        TEXT NOT NULL,
    mood           SMALLINT CHECK (mood IS NULL OR mood BETWEEN 1 AND 5),
    category       VARCHAR(50),
    status         SMALLINT NOT NULL DEFAULT 1,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE mind.journal_entries
IS 'Free-form journal entries with an optional title, mood rating, and category label.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN mind.journal_entries.id IS 'Unique identifier for each journal entry record (primary key).';

COMMENT ON COLUMN mind.journal_entries.user_id IS 'References the user who wrote this entry (foreign key to users.id).';

COMMENT ON COLUMN mind.journal_entries.entry_date IS 'Date the journal entry was written for.';

COMMENT ON COLUMN mind.journal_entries.title IS 'Optional short title for the entry.';

COMMENT ON COLUMN mind.journal_entries.content IS 'Full text of the journal entry.';

COMMENT ON COLUMN mind.journal_entries.mood IS 'Optional self-reported mood at the time of writing, on a 1-5 scale.';

COMMENT ON COLUMN mind.journal_entries.category
IS 'Optional free-text category label (e.g., Gratitude, Reflection, Goals).';

COMMENT ON COLUMN mind.journal_entries.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN mind.journal_entries.created_at IS 'Timestamp when this entry record was created.';
