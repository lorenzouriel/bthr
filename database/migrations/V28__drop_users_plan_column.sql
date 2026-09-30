------------------------------------------------------------
-- USERS TABLE ALIGNMENT WITH DBA-LAB SQL SERVER SCHEMA
-- The dba-lab reference schema has no subscription-plan column
-- on users; drop it here to match.
------------------------------------------------------------
ALTER TABLE users DROP COLUMN plan;
