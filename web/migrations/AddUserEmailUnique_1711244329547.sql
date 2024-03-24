-- ---------- MIGRONDI:UP:1711244329547 --------------
-- Write your Up migrations here
ALTER TABLE users ADD CONSTRAINT unique_email UNIQUE (email);
-- ---------- MIGRONDI:DOWN:1711244329547 --------------
-- Write how to revert the migration here
ALTER TABLE users DROP CONSTRAINT unique_email;
