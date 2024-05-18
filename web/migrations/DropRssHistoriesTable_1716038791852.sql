-- ---------- MIGRONDI:UP:1716038791852 --------------
-- Write your Up migrations here
DROP TABLE rss_histories;
-- ---------- MIGRONDI:DOWN:1716038791852 --------------
-- Write how to revert the migration here
CREATE TABLE rss_histories (
	id varchar(36) NOT NULL,
	url text NOT NULL,
    latest_updated timestamp without time zone NOT NULL,
	CONSTRAINT rss_histories_pk PRIMARY KEY (id),
    CONSTRAINT unique_url UNIQUE (url)
);