-- ---------- MIGRONDI:UP:1711254211072 --------------
-- Write your Up migrations here
ALTER TABLE rss_histories ADD CONSTRAINT unique_url UNIQUE (url);
-- ---------- MIGRONDI:DOWN:1711254211072 --------------
-- Write how to revert the migration here
ALTER TABLE rss_histories DROP CONSTRAINT unique_url;
