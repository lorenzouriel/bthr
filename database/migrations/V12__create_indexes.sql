------------------------------------------------------------
-- INDEXES FOR ALL TABLES
------------------------------------------------------------
-- Description: Creates indexes for optimizing query performance
-- on foreign key columns and commonly queried fields.
------------------------------------------------------------

-- Bills Indexes
CREATE INDEX IX_bills_user_id ON dbo.bills(user_id);
GO

-- Budgets Indexes
CREATE INDEX IX_budgets_user_id ON dbo.budgets(user_id);
GO

-- Goals Indexes
CREATE INDEX IX_goals_user_id ON dbo.goals(user_id);
GO

-- Investments Indexes
CREATE INDEX IX_investments_user_id ON dbo.investments(user_id);
GO

-- Expenses Indexes
CREATE INDEX IX_expenses_user_id ON dbo.expenses(user_id);
GO

-- Earnings Indexes
CREATE INDEX IX_earnings_user_id ON dbo.earnings(user_id);
GO
