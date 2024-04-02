-- ---------- MIGRONDI:UP:1712048460557 --------------
-- Write your Up migrations here
ALTER TABLE rss_histories DROP COLUMN latest_title RESTRICT;
-- ---------- MIGRONDI:DOWN:1712048460557 --------------
-- Write how to revert the migration here
ALTER TABLE rss_histories ADD COLUMN IF NOT EXISTS latest_title text DEFAULT '' NOT NULL;
