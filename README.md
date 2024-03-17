# RSS Bokmarkr

Bookmark your favorite RSS feeds.

## Development

### Prerequisite Tools

- [Bun](https://github.com/oven-sh/bun) 1.0
- [.NET](https://dotnet.microsoft.com/en-us/download) 8.0

### Why need Bun?

Because the Client app use [Feliz](https://github.com/Zaid-Ajaj/Feliz) and it's resulting code that depends to React in the end, also the Client app use [Daisy UI](https://daisyui.com/) ( [Feliz.DaisyUI](https://dzoukr.github.io/Feliz.DaisyUI/#/) ) and it's depends to [Tailwind](https://tailwindcss.com/), and some other tooling that need to be installed from NPM like [Elmish.Debbuger](https://github.com/elmish/debugger) depends to [remotedev](https://github.com/zalmoxisus/remotedev). The reason to choose [Bun](https://github.com/oven-sh/bun) instead of `npm` or other package manage ([Node.js](https://nodejs.org/en)) is because [Bun](https://github.com/oven-sh/bun) is such a cool project.

### Why need .NET?

It's obvious, tooling and to run F# need it.

### Install dependencies

```bash
# Install dependencies for Client app
bun install


# Install dependencies for Server app
dotnet tool restore
dotnet restore
dotnet paket restore
```

Use [Paket](https://fsprojects.github.io/Paket/) for dependency manager for the Server app.

Just because most (maybe all) F# app found out there use it, but it's such a cool project.

### Start Development

See the script in [package.json here](./web/package.json) and in [Makefile here](./web/Makefile)

```bash
bun run start
```

## Migration

```bash
cd web
```

Create `migrondi.json`, see [Migrondi](https://github.com/AngelMunoz/migrondi).

```bash
cp migrondi.example.json migrondi.json
```

Edit the `connection` in `migrondi.json`, fill the _blank_ value. After that run the migration.

```bash
make migrate_up
```

## Deployment

### Using Docker

See the [Dockerfile here](./web/Dockerfile).

```bash
cd web
```

Create `.env` file, [see example](./web/.env.example).

```env
ConnectionStrings__RssDb=<POSTGRES CONNECTION STRING>
MailSettings__Server=<YOUR MAIL SERVER HOST>
MailSettings__Port=<YOUR MAIL SERVER PORT>
MailSettings__SenderName=<YOUR_NAME>
MailSettings__SenderEmail=<YOUR@EMAIL>
MailSettings__UserName=<YOUR MAIL SERVER USERNAME>
MailSettings__Password=<YOUR MAIL SERVER PASWORD>
PORT=<PUBLISHED PORT FOR THE SERVER APP INSIDE Docker> # Used for docker-compose
ASPNETCORE_URLS_PORT=<SERVER APP PORT INSIDE Docker>
ASPNETCORE_URLS=<SERVER APP HOST AND PORT INSIDE Docker>
```

#### Using `docker compose up`

See the [docker-compose.yaml here](./web/docker-compose.yaml).

```bash
docker compose up
```

#### Using `docker build` and `docker run`

```bash
# Build
docker build -t rss-bookmarkr -f ./Dockerfile .

# Run
docker run --env-file ./.env -p <PUBLISHD PORT>:<SERVER APP PORT INSIDE Docker> rss-bookmarkr
## or
docker run \
-e ConnectionStrings__RssDb="POSTGRES CONNECTION STRING" \
-e MailSettings__Server=<YOUR MAIL SERVER HOST> \
-e MailSettings__Port=<YOUR MAIL SERVER PORT> \
-e MailSettings__SenderName=<YOUR_NAME> \
-e MailSettings__SenderEmail=<YOUR@EMAIL> \
-e MailSettings__UserName=<YOUR MAIL SERVER USERNAME> \
-e MailSettings__Password=<YOUR MAIL SERVER PASWORD> \
-e ASPNETCORE_URLS="<SERVER APP HOST AND PORT INSIDE Docker>" \
-p <PUBLISHD PORT>:<SERVER APP PORT INSIDE Docker> \
rss-bookmarkr
```
