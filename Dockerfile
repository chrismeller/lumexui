# -----------------------------
# Stage 1: build/publish
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Fail a RUN step when any command in a pipe fails, not only the last one.
SHELL ["/bin/bash", "-euo", "pipefail", "-c"]

# Node and pnpm at the versions mise.toml pins, so the image builds with the same tools as CI and local builds.
# Node comes from nodejs.org and is checked against its published SHA-256.
COPY mise.toml /tmp/mise.toml
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates xz-utils \
    && apt-get clean && rm -rf /var/lib/apt/lists/* \
    && node_version="$(sed -n 's/^node = "\(.*\)"$/\1/p' /tmp/mise.toml)" \
    && pnpm_version="$(sed -n 's/^pnpm = "\(.*\)"$/\1/p' /tmp/mise.toml)" \
    && test -n "$node_version" && test -n "$pnpm_version" \
    && case "$(dpkg --print-architecture)" in amd64) arch=x64 ;; arm64) arch=arm64 ;; *) exit 1 ;; esac \
    && tarball="node-v${node_version}-linux-${arch}.tar.xz" \
    && curl -fsSLO "https://nodejs.org/dist/v${node_version}/${tarball}" \
    && curl -fsSL "https://nodejs.org/dist/v${node_version}/SHASUMS256.txt" | grep " ${tarball}\$" | sha256sum -c - \
    && tar -xJf "$tarball" -C /usr/local --strip-components=1 --no-same-owner \
    && rm "$tarball" /tmp/mise.toml \
    && npm install -g "pnpm@${pnpm_version}" \
    && node --version && pnpm --version

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