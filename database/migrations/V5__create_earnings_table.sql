------------------------------------------------------------
-- DBO.EARNINGS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE dbo.earnings (
    id INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
    user_id INT NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    category NVARCHAR(100) NOT NULL,
    description NVARCHAR(500) NULL,
    earning_date DATETIME NOT NULL,
    payment_method NVARCHAR(100) NOT NULL,
    currency_code NVARCHAR(10) NOT NULL DEFAULT 'BRL',
    status SMALLINT NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT GETDATE()
);
GO

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Records user earnings, such as salary, bonuses, or other income sources.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings';
GO

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

-- id
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Unique identifier for each earning record (primary key).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings',
    @level2type = N'Column', @level2name = N'id';
GO

-- user_id
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'References the user who owns the earning record.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings',
    @level2type = N'Column', @level2name = N'user_id';
GO

-- amount
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Total amount of money earned in the transaction.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings',
    @level2type = N'Column', @level2name = N'amount';
GO

-- category
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Category describing the earning source (e.g., Salary, Bonus, Freelance).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings',
    @level2type = N'Column', @level2name = N'category';
GO

-- description
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Optional text describing the earning or its source in more detail.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings',
    @level2type = N'Column', @level2name = N'description';
GO

-- earning_date
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Date and time when the earning was received or recorded.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings',
    @level2type = N'Column', @level2name = N'earning_date';
GO

-- payment_method
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Method by which the earning was received (e.g., Bank Transfer, Cash, Card).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings',
    @level2type = N'Column', @level2name = N'payment_method';
GO

-- currency_code
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Currency code for the earning (e.g., USD, BRL, EUR).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings',
    @level2type = N'Column', @level2name = N'currency_code';
GO

-- status
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Indicates if the earning record is active (1), inactive (0), or archived.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings',
    @level2type = N'Column', @level2name = N'status';
GO

-- created_at
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Date and time when the earning record was created in the system.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'earnings',
    @level2type = N'Column', @level2name = N'created_at';
GO
