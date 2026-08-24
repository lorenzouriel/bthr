------------------------------------------------------------
-- DBO.BILLS TABLE DEFINITION
-- Bills are stored as templates (one row per bill definition).
-- Payment tracking is done via the expenses table.
------------------------------------------------------------
CREATE TABLE dbo.bills (
    id              INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
    user_id         INT NOT NULL,
    name            NVARCHAR(255) NOT NULL,
    description     NVARCHAR(500) NULL,
    category        NVARCHAR(100) NOT NULL,
    amount          DECIMAL(18,2) NOT NULL,
    due_day         TINYINT NOT NULL,
    payment_method  NVARCHAR(100) NULL,
    currency_code   NVARCHAR(10) NOT NULL DEFAULT 'BRL',
    is_recurrent    BIT NOT NULL DEFAULT 1,
    end_date        DATE NULL,
    recurrence_type NVARCHAR(50) NULL,
    status          SMALLINT NOT NULL DEFAULT 1,
    created_at      DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_bills_due_day CHECK (due_day BETWEEN 1 AND 31)
);
GO

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Stores bill templates for users. One row per bill definition. Payment tracking is done via the expenses table (matched by description = bill name).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills';
GO

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Unique identifier for each bill template (primary key).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'id';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'References the user who owns this bill.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'user_id';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Descriptive name of the bill (e.g., Netflix, Conta de Luz). Used to match payments in expenses table.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'name';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Optional description or notes for this bill.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'description';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Category of the bill (e.g., Moradia, Lazer, Saúde).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'category';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Monetary amount due for this bill.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'amount';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Day of month the bill is due (1-31). The API computes the full due date for the current month.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'due_day';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Payment method (e.g., Cartão de Crédito, Pix, Débito Automático).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'payment_method';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Currency code (e.g., BRL, USD).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'currency_code';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Whether the bill recurs monthly. 1=recurring (end_date must be NULL), 0=one-time.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'is_recurrent';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Optional end date for non-recurring bills. NULL for recurring bills.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'end_date';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Recurrence pattern (e.g., Monthly, Yearly).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'recurrence_type';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Status flag (1=Active, 0=Deleted).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'status';
GO

EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Timestamp when the bill template was created.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'bills',
    @level2type = N'Column', @level2name = N'created_at';
GO
