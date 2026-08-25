------------------------------------------------------------
-- USERS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE users
(
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    username VARCHAR(100) NOT NULL,
    phone_number VARCHAR(15),
    email VARCHAR(100) NOT NULL,
    password VARCHAR(1024),
    created_at TIMESTAMPTZ DEFAULT now() NOT NULL,
    plan SMALLINT NOT NULL DEFAULT 0,
    status SMALLINT DEFAULT 1 NOT NULL
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE users IS 'Stores registered application users and their authentication data.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

-- id
COMMENT ON COLUMN users.id IS 'Unique identifier for each user (primary key).';

-- username
COMMENT ON COLUMN users.username IS 'Public username chosen by the user (unique within the system).';

-- phone_number
COMMENT ON COLUMN users.phone_number IS 'Optional phone number used for contact or verification.';

-- email
COMMENT ON COLUMN users.email IS 'Primary email address of the user (used for login and notifications).';

-- password
COMMENT ON COLUMN users.password IS 'Hashed password for authentication (never stored in plain text).';

-- created_at
COMMENT ON COLUMN users.created_at IS 'Date and time when the user record was created.';

-- plan
COMMENT ON COLUMN users.plan IS 'Subscription plan (0=Freemium, 1=Basic).';

-- status
COMMENT ON COLUMN users.status IS 'User status flag (1 = active, 0 = inactive, others for future states).';
