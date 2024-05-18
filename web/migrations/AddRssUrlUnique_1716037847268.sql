-- ---------- MIGRONDI:UP:1716037847268 --------------
-- Write your Up migrations here
ALTER TABLE rss_urls ADD CONSTRAINT unique_rss_url UNIQUE (url, user_id);
-- ---------- MIGRONDI:DOWN:1716037847268 --------------
-- Write how to revert the migration here
ALTER TABLE rss_urls DROP CONSTRAINT unique_rss_url;
