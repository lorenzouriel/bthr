------------------------------------------------------------
-- BUDGETS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE budgets
(
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    description VARCHAR(500),
    amount_limit NUMERIC(18,2) NOT NULL,
    currency_code VARCHAR(10) NOT NULL DEFAULT 'BRL',
    start_date TIMESTAMPTZ NOT NULL,
    end_date TIMESTAMPTZ NOT NULL,
    status SMALLINT NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE budgets IS 'Defines user-created financial budgets with spending limits and date ranges.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

-- id
COMMENT ON COLUMN budgets.id IS 'Unique identifier for each budget (primary key).';

-- user_id
COMMENT ON COLUMN budgets.user_id IS 'Identifier of the user who owns or created the budget.';

-- name
COMMENT ON COLUMN budgets.name IS 'Name assigned to the budget (e.g., "Vacation 2025" or "Monthly Groceries").';

-- description
COMMENT ON COLUMN budgets.description IS 'Optional detailed description of the budget purpose or scope.';

-- amount_limit
COMMENT ON COLUMN budgets.amount_limit IS 'Total monetary limit allocated for this budget period.';

-- currency_code
COMMENT ON COLUMN budgets.currency_code IS 'Currency associated with the budget (e.g., USD, BRL, EUR).';

-- start_date
COMMENT ON COLUMN budgets.start_date IS 'Date and time when the budget becomes active.';

-- end_date
COMMENT ON COLUMN budgets.end_date IS 'Date and time when the budget period ends.';

-- status
COMMENT ON COLUMN budgets.status IS 'Indicates whether the budget is active (1), inactive (0), or archived.';

-- created_at
COMMENT ON COLUMN budgets.created_at IS 'Timestamp when the budget record was created.';
