-- ---------- MIGRONDI:UP:1708244368148 --------------
-- Write your Up migrations here
CREATE TABLE rss_urls (
	id varchar(36) NOT NULL,
	url text NOT NULL,
	user_id varchar(36) NOT NULL,
	CONSTRAINT rss_urls_pk PRIMARY KEY (id),
    FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE ON UPDATE CASCADE
);
-- ---------- MIGRONDI:DOWN:1708244368148 --------------
-- Write how to revert the migration here
DROP TABLE rss_urls;
