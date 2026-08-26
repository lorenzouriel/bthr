------------------------------------------------------------
-- MEALS TABLE DEFINITION
-- Per-meal nutrition log. Daily totals are computed by summing
-- this table's rows for a given date/user, not stored separately.
------------------------------------------------------------
CREATE TABLE body.meals (
    id             INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id        INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    meal_date      DATE NOT NULL,
    meal_type      VARCHAR(50) NOT NULL,
    description    VARCHAR(500),
    calories       NUMERIC(8,2) NOT NULL,
    protein_grams  NUMERIC(6,2),
    carbs_grams    NUMERIC(6,2),
    fat_grams      NUMERIC(6,2),
    status         SMALLINT NOT NULL DEFAULT 1,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE body.meals
IS 'Per-meal nutrition log. Daily totals are computed by summing this table''s rows for a given '
'date/user, not stored separately.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN body.meals.id IS 'Unique identifier for each meal record (primary key).';

COMMENT ON COLUMN body.meals.user_id IS 'References the user who logged this meal (foreign key to users.id).';

COMMENT ON COLUMN body.meals.meal_date IS 'Date the meal was consumed.';

COMMENT ON COLUMN body.meals.meal_type
IS 'Type of meal (e.g., Breakfast, Lunch, Dinner, Snack).';

COMMENT ON COLUMN body.meals.description IS 'Optional description of what was eaten.';

COMMENT ON COLUMN body.meals.calories IS 'Total calories for this meal.';

COMMENT ON COLUMN body.meals.protein_grams IS 'Grams of protein in this meal.';

COMMENT ON COLUMN body.meals.carbs_grams IS 'Grams of carbohydrates in this meal.';

COMMENT ON COLUMN body.meals.fat_grams IS 'Grams of fat in this meal.';

COMMENT ON COLUMN body.meals.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN body.meals.created_at IS 'Timestamp when this meal record was created.';
