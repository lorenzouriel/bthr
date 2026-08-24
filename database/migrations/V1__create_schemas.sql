------------------------------------------------------------
-- CREATE DATABASE SCHEMAS
------------------------------------------------------------
-- Description: Initialize all schemas for the finance application
-- Author: Database Team
-- Date: 2025-11-06
------------------------------------------------------------

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'finance')
BEGIN
    EXEC('CREATE SCHEMA [finance]');
END
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'plan')
BEGIN
    EXEC('CREATE SCHEMA [plan]');
END
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'reporting')
BEGIN
    EXEC('CREATE SCHEMA [reporting]');
END
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'investment')
BEGIN
    EXEC('CREATE SCHEMA [investment]');
END
GO
