-- ---------- MIGRONDI:UP:1713069305375 --------------
-- Write your Up migrations here
ALTER TABLE users ALTER COLUMN email DROP NOT NULL;
ALTER TABLE users ALTER COLUMN email DROP DEFAULT;
ALTER TABLE users ALTER COLUMN email SET DEFAULT NULL;
ALTER TABLE users DROP CONSTRAINT unique_email;
ALTER TABLE users ADD CONSTRAINT unique_email UNIQUE NULLS NOT DISTINCT (id, email);
-- ---------- MIGRONDI:DOWN:1713069305375 --------------
-- Write how to revert the migration here
ALTER TABLE users DROP CONSTRAINT unique_email;
ALTER TABLE users ADD CONSTRAINT unique_email UNIQUE (email);
ALTER TABLE users ALTER COLUMN email DROP DEFAULT;
ALTER TABLE users ALTER COLUMN email SET DEFAULT '';
ALTER TABLE users ALTER COLUMN email SET NOT NULL;

