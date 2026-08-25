------------------------------------------------------------
-- EARNINGS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE earnings (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id INT NOT NULL,
    amount NUMERIC(18,2) NOT NULL,
    category VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    earning_date TIMESTAMPTZ NOT NULL,
    payment_method VARCHAR(100) NOT NULL,
    currency_code VARCHAR(10) NOT NULL DEFAULT 'BRL',
    status SMALLINT NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE earnings IS 'Records user earnings, such as salary, bonuses, or other income sources.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

-- id
COMMENT ON COLUMN earnings.id IS 'Unique identifier for each earning record (primary key).';

-- user_id
COMMENT ON COLUMN earnings.user_id IS 'References the user who owns the earning record.';

-- amount
COMMENT ON COLUMN earnings.amount IS 'Total amount of money earned in the transaction.';

-- category
COMMENT ON COLUMN earnings.category IS 'Category describing the earning source (e.g., Salary, Bonus, Freelance).';

-- description
COMMENT ON COLUMN earnings.description IS 'Optional text describing the earning or its source in more detail.';

-- earning_date
COMMENT ON COLUMN earnings.earning_date IS 'Date and time when the earning was received or recorded.';

-- payment_method
COMMENT ON COLUMN earnings.payment_method
IS 'Method by which the earning was received (e.g., Bank Transfer, Cash, Card).';

-- currency_code
COMMENT ON COLUMN earnings.currency_code IS 'Currency code for the earning (e.g., USD, BRL, EUR).';

-- status
COMMENT ON COLUMN earnings.status IS 'Indicates if the earning record is active (1), inactive (0), or archived.';

-- created_at
COMMENT ON COLUMN earnings.created_at IS 'Date and time when the earning record was created in the system.';
