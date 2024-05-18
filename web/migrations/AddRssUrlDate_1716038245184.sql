-- ---------- MIGRONDI:UP:1716038245184 --------------
-- Write your Up migrations here
ALTER TABLE rss_urls ADD COLUMN IF NOT EXISTS latest_updated timestamp without time zone NOT NULL DEFAULT current_timestamp;
-- ---------- MIGRONDI:DOWN:1716038245184 --------------
-- Write how to revert the migration here
ALTER TABLE rss_urls DROP COLUMN IF EXISTS latest_updated RESTRICT;
