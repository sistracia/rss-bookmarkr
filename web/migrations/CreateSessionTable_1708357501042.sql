-- ---------- MIGRONDI:UP:1708357501042 --------------
-- Write your Up migrations here
CREATE TABLE sessions (
	id varchar(36) NOT NULL,
	user_id varchar(36) NOT NULL,
	CONSTRAINT sessions_pk PRIMARY KEY (id),
    FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE ON UPDATE CASCADE
);
-- ---------- MIGRONDI:DOWN:1708357501042 --------------
-- Write how to revert the migration here
DROP TABLE sessions;
