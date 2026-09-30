------------------------------------------------------------
-- INVESTMENTS TABLE ALIGNMENT WITH DBA-LAB SQL SERVER SCHEMA
-- The dba-lab reference schema does not persist current value,
-- profit/loss, or yield: "Current value, returns, and price
-- history are not stored here -- they are computed by the
-- application from a market-data API at read time." Drop the
-- three stored columns here to match.
------------------------------------------------------------
ALTER TABLE investments
    DROP COLUMN current_value,
    DROP COLUMN profit_loss,
    DROP COLUMN annual_yield_percent;
