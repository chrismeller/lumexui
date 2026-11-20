# -----------------------------
# Stage 1: build/publish
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install Node.js v22 (LTS current)
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y nodejs \
    && apt-get clean && rm -rf /var/lib/apt/lists/* \
    && corepack enable

WORKDIR /src

COPY . .
# LumexUI.Motion restores from GitHub Packages, which needs a token with read:packages:
#   docker build --secret id=github_token,env=GITHUB_TOKEN .
RUN --mount=type=secret,id=github_token,required=true \
    NuGetPackageSourceCredentials_chrismeller="Username=docker;Password=$(cat /run/secrets/github_token)" \
    dotnet publish ./docs/LumexUI.Docs -c Release -o app
RUN node ./scripts/export-static.mjs app/LumexUI.Docs.dll /static

# -----------------------------
# Stage 2: static site
# -----------------------------
FROM alpine:3
COPY --from=build /static /static