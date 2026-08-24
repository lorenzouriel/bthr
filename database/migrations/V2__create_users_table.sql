------------------------------------------------------------
-- USERS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE dbo.users
(
    id INT IDENTITY (1, 1) PRIMARY KEY NOT NULL,
    username VARCHAR(100) NOT NULL,
    phone_number VARCHAR(15) NULL,
    email VARCHAR(100) NOT NULL,
    [password] NVARCHAR(1024) NULL,
    created_at DATETIME DEFAULT GETDATE() NOT NULL,
    [plan] TINYINT NOT NULL DEFAULT 0,
    [status] TINYINT DEFAULT 1 NOT NULL
);
GO

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Stores registered application users and their authentication data.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'users';
GO

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

-- id
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Unique identifier for each user (primary key).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'users',
    @level2type = N'Column', @level2name = N'id';
GO

-- username
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Public username chosen by the user (unique within the system).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'users',
    @level2type = N'Column', @level2name = N'username';
GO

-- phone_number
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Optional phone number used for contact or verification.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'users',
    @level2type = N'Column', @level2name = N'phone_number';
GO

-- email
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Primary email address of the user (used for login and notifications).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'users',
    @level2type = N'Column', @level2name = N'email';
GO

-- password
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Hashed password for authentication (never stored in plain text).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'users',
    @level2type = N'Column', @level2name = N'password';
GO

-- created_at
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Date and time when the user record was created.',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'users',
    @level2type = N'Column', @level2name = N'created_at';
GO

-- plan
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Subscription plan (0=Freemium, 1=Basic).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'users',
    @level2type = N'Column', @level2name = N'plan';
GO

-- status
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'User status flag (1 = active, 0 = inactive, others for future states).',
    @level0type = N'Schema', @level0name = N'dbo',
    @level1type = N'Table',  @level1name = N'users',
    @level2type = N'Column', @level2name = N'status';
GO
