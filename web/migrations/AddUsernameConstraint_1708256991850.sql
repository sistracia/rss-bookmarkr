-- ---------- MIGRONDI:UP:1708256991850 --------------
-- Write your Up migrations here
ALTER TABLE users ADD CONSTRAINT unique_username UNIQUE (username);
-- ---------- MIGRONDI:DOWN:1708256991850 --------------
-- Write how to revert the migration here
ALTER TABLE users DROP CONSTRAINT unique_username;
