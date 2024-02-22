# RSS Bokmarkr

Bookmark your favorite RSS feeds.

## Development

### Prerequisite Tools

- [Bun](https://github.com/oven-sh/bun) 1.0
- [.NET](https://dotnet.microsoft.com/en-us/download) 8.0

### Why need Bun?

Because the Client app use [Feliz](https://github.com/Zaid-Ajaj/Feliz) and it's resulting code that depends to React in the end.

Also the Client app use [Daisy UI](https://daisyui.com/) ( [Feliz.DaisyUI](https://dzoukr.github.io/Feliz.DaisyUI/#/) ) and it's depends to [Tailwind](https://tailwindcss.com/).

And some other tooling that need to be installed from NPM like [Elmish.Debbuger](https://github.com/elmish/debugger) depends to [remotedev](https://github.com/zalmoxisus/remotedev).

The reaon to choose [Bun](https://github.com/oven-sh/bun) instead of `npm` or other package manage ([Node.js](https://nodejs.org/en)) is because [Bun](https://github.com/oven-sh/bun) is such a cool project.

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
DB_CONNECTION_STRING=<POSTGRES CONNECTION STRING>
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
docker run --env-file ./env -p <PUBLISHD PORT>:<SERVER APP PORT INSIDE Docker> rss-bookmarkr
## or
docker run \
-e DB_CONNECTION_STRING="POSTGRES CONNECTION STRING" \
-e ASPNETCORE_URLS="<SERVER APP HOST AND PORT INSIDE Docker>" \
-p <PUBLISHD PORT>:<SERVER APP PORT INSIDE Docker> \
rss-bookmarkr
```

## My Note

This project is my first toy project using F#. I made it for learning purpose, still on the very tip of iceberg.

Also currently I learning mobile development using Jetpack Compose and SwiftUI, and I'll use the API from the `web` app.

I feel the `web` app codebase getting unreadable as it's grow, maybe I will comeback later when I already good in F#

My thought of F# is it's cool, it's my first functional paradigm language I learned, and I like it.

## TODO I Want to Try
- [ ] Clean Up Codebase
- [ ] Add Subscription Feature
- [ ] Try FAKE instead of Makefile
- [ ] Build Android Client with Jetpack Compose
- [ ] Build Swift Client with SwiftUI
- [ ] Try Kotlin Multiplatform
