------------------------------------------------------------
-- INDEXES FOR ALL TABLES
------------------------------------------------------------
-- Description: Creates indexes for optimizing query performance
-- on foreign key columns and commonly queried fields.
------------------------------------------------------------

-- Bills Indexes
CREATE INDEX ix_bills_user_id ON bills (user_id);

-- Budgets Indexes
CREATE INDEX ix_budgets_user_id ON budgets (user_id);

-- Goals Indexes
CREATE INDEX ix_goals_user_id ON goals (user_id);

-- Investments Indexes
CREATE INDEX ix_investments_user_id ON investments (user_id);

-- Expenses Indexes
CREATE INDEX ix_expenses_user_id ON expenses (user_id);

-- Earnings Indexes
CREATE INDEX ix_earnings_user_id ON earnings (user_id);
