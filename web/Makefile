error:
	echo "Available option: server, client"

server:
	dotnet watch run --project src/Server

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
	docker compose -f docker-compose.e2e.yaml up --build --abort-on-container-exit --exit-code-from e2e

test_e2e_teardown:
	docker compose -f docker-compose.e2e.yaml down

test_e2e: test_e2e_setup test_e2e_teardown