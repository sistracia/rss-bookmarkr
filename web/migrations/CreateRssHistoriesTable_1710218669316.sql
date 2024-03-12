-- ---------- MIGRONDI:UP:1710218669316 --------------
-- Write your Up migrations here
CREATE TABLE rss_histories (
	id varchar(36) NOT NULL,
	url text NOT NULL,
	latest_title text NOT NULL,
    latest_updated timestamp without time zone NOT NULL,
	CONSTRAINT rss_histories_pk PRIMARY KEY (id)
);
-- ---------- MIGRONDI:DOWN:1710218669316 --------------
-- Write how to revert the migration here
DROP TABLE rss_histories;
