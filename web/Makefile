error:
	echo "Available option: server, client"

server:
    # Temporary use --no-hot-reload, https://github.com/dotnet/sdk/issues/44908
	dotenvx run -- dotnet watch --no-hot-reload run --project src/Server

client:
	dotnet fable watch --cwd src/Client -o output -s --run bunx vite

style:
	bunx tailwindcss -i ./src/Client/input.css -o ./src/Client/output/output.css --watch

migrate_up:
	dotnet migrondi up --dry-run false

build_fable:
	dotnet fable --cwd src/Client -o output

build_style:
	bunx tailwindcss -i ./src/Client/input.css -o ./src/Client/output/output.css --minify

build_client:
	bunx vite build ./src/Client

publish_client: build_fable build_style build_client

publish_server:
	dotnet publish src/Server -c Release -o publish

copy_client:
	cp -R ./src/Client/dist publish/wwwroot

copy_email:
	cp -R ./src/Server/Templates publish/Templates

publish_project: publish_client publish_server copy_client copy_email

test_e2e_setup:
	docker compose -p rss-bookmarkr -f docker-compose.e2e.yaml up --build -d

test_e2e_migration_backup:
	mv migrondi.json migrondi.bak.json ;\
	mv migrondi.e2e.json migrondi.json

test_e2e_migration_restore:
	mv migrondi.json migrondi.e2e.json ;\
	mv migrondi.bak.json migrondi.json

test_e2e_migration: test_e2e_migration_backup migrate_up test_e2e_migration_restore

test_e2e_teardown:
	docker compose -p rss-bookmarkr -f docker-compose.e2e.yaml down

test_unit_server:
	dotnet run --project src/Server.Test