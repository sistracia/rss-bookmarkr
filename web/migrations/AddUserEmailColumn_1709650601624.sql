-- ---------- MIGRONDI:UP:1709650601624 --------------
-- Write your Up migrations here
ALTER TABLE users ADD COLUMN IF NOT EXISTS email varchar(50) DEFAULT '' NOT NULL;
-- ---------- MIGRONDI:DOWN:1709650601624 --------------
-- Write how to revert the migration here
ALTER TABLE users DROP COLUMN IF EXISTS email RESTRICT;
