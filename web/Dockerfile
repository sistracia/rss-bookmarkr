## Server Build Area
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS server-build
WORKDIR /Server

# Copy everything
COPY . .
# Restore as distinct layers
RUN dotnet tool restore
RUN dotnet restore
RUN dotnet paket restore
# Build and publish a release
RUN dotnet fable --noCache --cwd src/Client -o output
RUN dotnet publish src/Server -c Release -o publish

## Client Build Area

# use the official Bun image
# see all versions at https://hub.docker.com/r/oven/bun/tags
FROM oven/bun:1 as client-base
WORKDIR /Client

# install dependencies into temp directory
# this will cache them and speed up future builds
FROM client-base AS client-install
RUN mkdir -p /temp/dev
COPY package.json bun.lockb /temp/dev/
RUN cd /temp/dev && bun install --frozen-lockfile

# install with --production (exclude devDependencies)
RUN mkdir -p /temp/prod
COPY package.json bun.lockb /temp/prod/
RUN cd /temp/prod && bun install --frozen-lockfile --production

# copy node_modules from temp directory
# then copy all (non-ignored) project files into the image
FROM client-base AS client-build
COPY --from=client-install /temp/dev/node_modules node_modules
COPY . .
COPY --from=server-build /Server/src/Client/output ./src/Client/output

ENV NODE_ENV=production
RUN bunx tailwindcss -i ./src/Client/input.css -o ./src/Client/output/output.css --minify
RUN bunx vite build ./src/Client

## App Area

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=client-build /Client/src/Client/dist wwwroot
COPY --from=server-build /Server/publish publish
ENTRYPOINT ["dotnet", "publish/Server.dll"]