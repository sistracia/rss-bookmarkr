error:
	echo "Available option: server, client"

server:
	dotnet watch run --project src/Server

client:
	dotnet fable watch --cwd src/Client -o output -s --run bunx vite

style:
	bunx tailwindcss -i ./src/Client/input.css -o ./src/Client/output/output.css --watch