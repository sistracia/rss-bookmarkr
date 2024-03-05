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

publish_client:
	dotnet fable --noCache --cwd src/Client -o output

publish_style:
	bunx tailwindcss -i ./src/Client/input.css -o ./src/Client/output/output.css --minify

build_style:
	bunx vite build ./src/Client

copy_client:
	mv ./src/Client/dist wwwroot

publish_server:
	dotnet publish src/Server -c Release -o publish

publish_project: publish_client publish_style build_style copy_client publish_server