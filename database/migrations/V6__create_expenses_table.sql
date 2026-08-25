------------------------------------------------------------
-- EXPENSES TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE expenses (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id INT NOT NULL,
    amount NUMERIC(18,2) NOT NULL,
    category VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    expense_date TIMESTAMPTZ NOT NULL,
    payment_method VARCHAR(100) NOT NULL,
    currency_code VARCHAR(10) NOT NULL DEFAULT 'BRL',
    status SMALLINT NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE expenses IS 'Records user expenses, including purchase details, amounts, and payment methods.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

-- id
COMMENT ON COLUMN expenses.id IS 'Unique identifier for each expense record (primary key).';

-- user_id
COMMENT ON COLUMN expenses.user_id IS 'References the user who owns the expense record.';

-- amount
COMMENT ON COLUMN expenses.amount IS 'Total monetary value of the expense transaction.';

-- category
COMMENT ON COLUMN expenses.category
IS 'Category describing the type of expense (e.g., Food, Transportation, Utilities).';

-- description
COMMENT ON COLUMN expenses.description IS 'Optional text providing more details about the expense or its context.';

-- expense_date
COMMENT ON COLUMN expenses.expense_date IS 'Date and time when the expense occurred or was recorded.';

-- payment_method
COMMENT ON COLUMN expenses.payment_method IS 'Method used for payment (e.g., Credit Card, Cash, Bank Transfer).';

-- currency_code
COMMENT ON COLUMN expenses.currency_code IS 'Currency code representing the expense currency (e.g., USD, BRL, EUR).';

-- status
COMMENT ON COLUMN expenses.status IS 'Indicates if the expense record is active (1), inactive (0), or archived.';

-- created_at
COMMENT ON COLUMN expenses.created_at IS 'Date and time when the expense record was created in the system.';
