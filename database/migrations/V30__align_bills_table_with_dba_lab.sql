------------------------------------------------------------
-- BILLS TABLE ALIGNMENT WITH DBA-LAB SQL SERVER SCHEMA
-- Replaces the due_day/is_recurrent/end_date "recurring template"
-- model with the dba-lab due_date/next_due_date/paid_date/
-- recurrence_interval "occurrence" model, and renames name to
-- bill_name to match.
--
-- due_date/next_due_date/paid_date are added nullable: due_day
-- was a day-of-month template (1-31) with no month/year, so there
-- is no safe value to backfill an absolute due_date from existing
-- rows. Enforce NOT NULL on due_date once the application starts
-- populating it.
------------------------------------------------------------

ALTER TABLE bills RENAME COLUMN name TO bill_name;

ALTER TABLE bills ALTER COLUMN description TYPE VARCHAR(255);

ALTER TABLE bills
    DROP COLUMN due_day,
    DROP COLUMN is_recurrent,
    DROP COLUMN end_date,
    ADD COLUMN due_date DATE,
    ADD COLUMN recurrence_interval INT DEFAULT 1,
    ADD COLUMN next_due_date DATE,
    ADD COLUMN paid_date DATE,
    ADD COLUMN updated_at TIMESTAMPTZ;

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE bills
IS 'Defines bills and recurring payment obligations for users, including due dates, amounts, and '
'status tracking.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN bills.bill_name
IS 'Descriptive name of the bill (e.g., Electricity, Internet, Rent).';

COMMENT ON COLUMN bills.due_date IS 'Date when the bill is due for payment.';

COMMENT ON COLUMN bills.recurrence_interval IS 'Interval between bill recurrences (e.g., every 2 months).';

COMMENT ON COLUMN bills.next_due_date IS 'Next scheduled due date for recurring bills.';

COMMENT ON COLUMN bills.paid_date IS 'Date when the bill was paid, if applicable.';

COMMENT ON COLUMN bills.updated_at IS 'Timestamp when the bill record was last updated.';
