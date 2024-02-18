-- ---------- MIGRONDI:UP:1708244353124 --------------
-- Write your Up migrations here
CREATE TABLE users (
	id varchar(36) NOT NULL,
	username varchar(50) NOT NULL,
	"password" text NOT NULL,
	CONSTRAINT users_pk PRIMARY KEY (id)
);
-- ---------- MIGRONDI:DOWN:1708244353124 --------------
-- Write how to revert the migration here
DROP TABLE users;
