------------------------------------------------------------
-- BILLS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE bills (
    id              INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id         INT NOT NULL,
    bill_name       VARCHAR(255) NOT NULL,
    description     VARCHAR(255),
    category        VARCHAR(100) NOT NULL,
    amount          NUMERIC(18,2) NOT NULL,
    due_date        DATE,
    payment_method  VARCHAR(100),
    currency_code   VARCHAR(10) NOT NULL DEFAULT 'BRL',
    recurrence_type VARCHAR(50),
    recurrence_interval INT DEFAULT 1,
    next_due_date   DATE,
    paid_date       DATE,
    status          SMALLINT NOT NULL DEFAULT 1,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE bills
IS 'Defines bills and recurring payment obligations for users, including due dates, amounts, and '
'status tracking.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN bills.id IS 'Unique identifier for each bill record (primary key).';

COMMENT ON COLUMN bills.user_id IS 'References the user who owns this bill.';

COMMENT ON COLUMN bills.description IS 'Optional description or notes for this bill.';

COMMENT ON COLUMN bills.category IS 'Category of the bill (e.g., Moradia, Lazer, Saúde).';

COMMENT ON COLUMN bills.amount IS 'Monetary amount due for this bill.';

COMMENT ON COLUMN bills.payment_method IS 'Payment method (e.g., Cartão de Crédito, Pix, Débito Automático).';

COMMENT ON COLUMN bills.currency_code IS 'Currency code (e.g., BRL, USD).';

COMMENT ON COLUMN bills.recurrence_type IS 'Recurrence pattern (e.g., Monthly, Yearly).';

COMMENT ON COLUMN bills.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN bills.created_at IS 'Timestamp when the bill record was created.';

COMMENT ON COLUMN bills.bill_name
IS 'Descriptive name of the bill (e.g., Electricity, Internet, Rent).';

COMMENT ON COLUMN bills.due_date IS 'Date when the bill is due for payment.';

COMMENT ON COLUMN bills.recurrence_interval IS 'Interval between bill recurrences (e.g., every 2 months).';

COMMENT ON COLUMN bills.next_due_date IS 'Next scheduled due date for recurring bills.';

COMMENT ON COLUMN bills.paid_date IS 'Date when the bill was paid, if applicable.';

COMMENT ON COLUMN bills.updated_at IS 'Timestamp when the bill record was last updated.';
