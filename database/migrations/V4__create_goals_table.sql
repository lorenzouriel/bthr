------------------------------------------------------------
-- GOALS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE goals (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    description VARCHAR(500),
    target_amount NUMERIC(18,2) NOT NULL,
    current_amount NUMERIC(18,2) NOT NULL DEFAULT 0,
    currency_code VARCHAR(10) NOT NULL DEFAULT 'BRL',
    due_date TIMESTAMPTZ NOT NULL,
    status SMALLINT NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE goals IS 'Tracks user-defined financial goals and their progress toward a target amount.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

-- id
COMMENT ON COLUMN goals.id IS 'Unique identifier for each goal (primary key).';

-- user_id
COMMENT ON COLUMN goals.user_id IS 'Identifier of the user who owns the goal.';

-- name
COMMENT ON COLUMN goals.name IS 'Descriptive name of the financial goal (e.g., "Emergency Fund" or "New Car").';

-- description
COMMENT ON COLUMN goals.description IS 'Optional details about the purpose or motivation behind the goal.';

-- target_amount
COMMENT ON COLUMN goals.target_amount IS 'Total amount the user aims to reach for this goal.';

-- current_amount
COMMENT ON COLUMN goals.current_amount IS 'Amount currently saved or achieved toward the target.';

-- currency_code
COMMENT ON COLUMN goals.currency_code IS 'Currency associated with the goal (e.g., USD, BRL, EUR).';

-- due_date
COMMENT ON COLUMN goals.due_date IS 'Target date and time by which the goal should be achieved.';

-- status
COMMENT ON COLUMN goals.status IS 'Indicates the goal state (1 = active, 0 = inactive, others for future use).';

-- created_at
COMMENT ON COLUMN goals.created_at IS 'Date and time when the goal record was created.';
