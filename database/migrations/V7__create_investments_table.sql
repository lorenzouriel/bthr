------------------------------------------------------------
-- INVESTMENTS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE investments (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id INT NOT NULL,
    asset_name VARCHAR(255) NOT NULL,
    investment_type VARCHAR(100) NOT NULL,
    category VARCHAR(100) NOT NULL,
    invested_amount NUMERIC(18,2) NOT NULL,
    current_value NUMERIC(18,2),
    profit_loss NUMERIC(18,2),
    annual_yield_percent NUMERIC(8,4),
    broker VARCHAR(255),
    purchase_date TIMESTAMPTZ NOT NULL,
    maturity_date TIMESTAMPTZ,
    currency_code VARCHAR(10) NOT NULL DEFAULT 'BRL',
    status SMALLINT NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE investments
IS 'Stores detailed records of user investments across multiple asset types and platforms.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

-- id
COMMENT ON COLUMN investments.id IS 'Unique identifier for each investment record (primary key).';

-- user_id
COMMENT ON COLUMN investments.user_id IS 'References the user who owns the investment.';

-- asset_name
COMMENT ON COLUMN investments.asset_name
IS 'Name or ticker symbol of the invested asset (e.g., PETR4, CDB Itaú, Tesouro IPCA+).';

-- investment_type
COMMENT ON COLUMN investments.investment_type
IS 'Investment type classification (e.g., Fixed Income, Variable Income, Crypto, Fund, Treasury, Real Estate).';

-- category
COMMENT ON COLUMN investments.category IS 'Subcategory within the investment type (e.g., CDB, Stocks, REITs).';

-- invested_amount
COMMENT ON COLUMN investments.invested_amount IS 'Total amount of money originally invested in the asset.';

-- current_value
COMMENT ON COLUMN investments.current_value IS 'Current market value of the investment, updated periodically.';

-- profit_loss
COMMENT ON COLUMN investments.profit_loss IS 'Profit or loss amount calculated as current_value minus invested_amount.';

-- annual_yield_percent
COMMENT ON COLUMN investments.annual_yield_percent IS 'Annual percentage yield or expected return of the investment.';

-- broker
COMMENT ON COLUMN investments.broker
IS 'Financial platform or broker managing the investment (e.g., XP, Nubank, Binance).';

-- purchase_date
COMMENT ON COLUMN investments.purchase_date IS 'Date and time when the investment was made or asset was acquired.';

-- maturity_date
COMMENT ON COLUMN investments.maturity_date
IS 'Date and time when the investment matures or ends (applicable mainly to fixed income).';

-- currency_code
COMMENT ON COLUMN investments.currency_code IS 'Currency code representing the investment currency.';

-- status
COMMENT ON COLUMN investments.status IS 'Indicates if the investment record is active (1), inactive (0), or archived.';

-- created_at
COMMENT ON COLUMN investments.created_at IS 'Timestamp when the investment record was created in the system.';
