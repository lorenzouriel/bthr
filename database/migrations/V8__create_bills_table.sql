------------------------------------------------------------
-- BILLS TABLE DEFINITION
-- Bills are stored as templates (one row per bill definition).
-- Payment tracking is done via the expenses table.
------------------------------------------------------------
CREATE TABLE bills (
    id              INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id         INT NOT NULL,
    name            VARCHAR(255) NOT NULL,
    description     VARCHAR(500),
    category        VARCHAR(100) NOT NULL,
    amount          NUMERIC(18,2) NOT NULL,
    due_day         SMALLINT NOT NULL,
    payment_method  VARCHAR(100),
    currency_code   VARCHAR(10) NOT NULL DEFAULT 'BRL',
    is_recurrent    BOOLEAN NOT NULL DEFAULT true,
    end_date        DATE,
    recurrence_type VARCHAR(50),
    status          SMALLINT NOT NULL DEFAULT 1,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT ck_bills_due_day CHECK (due_day BETWEEN 1 AND 31)
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE bills
IS 'Stores bill templates for users. One row per bill definition. Payment tracking is done via '
'the expenses table (matched by description = bill name).';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN bills.id IS 'Unique identifier for each bill template (primary key).';

COMMENT ON COLUMN bills.user_id IS 'References the user who owns this bill.';

COMMENT ON COLUMN bills.name
IS 'Descriptive name of the bill (e.g., Netflix, Conta de Luz). Used to match payments in expenses table.';

COMMENT ON COLUMN bills.description IS 'Optional description or notes for this bill.';

COMMENT ON COLUMN bills.category IS 'Category of the bill (e.g., Moradia, Lazer, Saúde).';

COMMENT ON COLUMN bills.amount IS 'Monetary amount due for this bill.';

COMMENT ON COLUMN bills.due_day
IS 'Day of month the bill is due (1-31). The API computes the full due date for the current month.';

COMMENT ON COLUMN bills.payment_method IS 'Payment method (e.g., Cartão de Crédito, Pix, Débito Automático).';

COMMENT ON COLUMN bills.currency_code IS 'Currency code (e.g., BRL, USD).';

COMMENT ON COLUMN bills.is_recurrent
IS 'Whether the bill recurs monthly. 1=recurring (end_date must be NULL), 0=one-time.';

COMMENT ON COLUMN bills.end_date IS 'Optional end date for non-recurring bills. NULL for recurring bills.';

COMMENT ON COLUMN bills.recurrence_type IS 'Recurrence pattern (e.g., Monthly, Yearly).';

COMMENT ON COLUMN bills.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN bills.created_at IS 'Timestamp when the bill template was created.';
