------------------------------------------------------------
-- DBO.BUDGETS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE dbo.budgets 
(
    id INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
    user_id INT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(500) NULL,
    amount_limit DECIMAL(18,2) NOT NULL,
    currency_code NVARCHAR(10) NOT NULL DEFAULT 'BRL',
    start_date DATETIME NOT NULL,
    end_date DATETIME NOT NULL,
    status SMALLINT NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT GETDATE()
);
GO

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Defines user-created financial budgets with spending limits and date ranges.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets';
GO

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

-- id
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Unique identifier for each budget (primary key).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets',
    @level2type = N'Column', @level2name = N'id';
GO

-- user_id
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Identifier of the user who owns or created the budget.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets',
    @level2type = N'Column', @level2name = N'user_id';
GO

-- name
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Name assigned to the budget (e.g., "Vacation 2025" or "Monthly Groceries").',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets',
    @level2type = N'Column', @level2name = N'name';
GO

-- description
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Optional detailed description of the budget purpose or scope.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets',
    @level2type = N'Column', @level2name = N'description';
GO

-- amount_limit
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Total monetary limit allocated for this budget period.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets',
    @level2type = N'Column', @level2name = N'amount_limit';
GO

-- currency_code
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Currency associated with the budget (e.g., USD, BRL, EUR).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets',
    @level2type = N'Column', @level2name = N'currency_code';
GO

-- start_date
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Date and time when the budget becomes active.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets',
    @level2type = N'Column', @level2name = N'start_date';
GO

-- end_date
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Date and time when the budget period ends.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets',
    @level2type = N'Column', @level2name = N'end_date';
GO

-- status
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Indicates whether the budget is active (1), inactive (0), or archived.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets',
    @level2type = N'Column', @level2name = N'status';
GO

-- created_at
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Timestamp when the budget record was created.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'budgets',
    @level2type = N'Column', @level2name = N'created_at';
GO
