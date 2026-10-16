ALTER TABLE users ADD COLUMN telegram_user_id BIGINT;
CREATE UNIQUE INDEX uq_users_telegram_user_id ON users (telegram_user_id) WHERE telegram_user_id IS NOT NULL;
