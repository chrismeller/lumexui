# LumexUI

An open-source UI component library that offers a set of beautifully designed Blazor UI components, all fully built with Tailwind CSS.

These components are designed to be not only aesthetically pleasing, but also highly customizable, allowing you to tailor them to meet your specific needs.

## About this fork

This is a maintained fork of [LumexUI/lumexui](https://github.com/LumexUI/lumexui), which is archived and no longer developed. Upstream's last release was 2.4.0.

The fork keeps the library building, tested and up to date. From 3.0 it targets .NET 10 only.

## Restoring the package

The fork is published to GitHub Packages, not nuget.org. The upstream packages are still on nuget.org under the same IDs (`LumexUI` 2.4.0 and `LumexUI.Motion` 0.0.4, both archived), so a project that only adds the feed can restore upstream's package instead of the fork's. Package source mapping prevents that.

`LumexUI` depends on `LumexUI.Motion`, which this fork also publishes to the same feed ([chrismeller/lumexui-motion](https://github.com/chrismeller/lumexui-motion)).

### 1. Add a `nuget.config` next to your solution

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="chrismeller" value="https://nuget.pkg.github.com/chrismeller/index.json" />
  </packageSources>
  <packageSourceMapping>
    <!-- The forks only ever come from the chrismeller feed. -->
    <packageSource key="chrismeller">
      <package pattern="LumexUI" />
      <package pattern="LumexUI.Motion" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

The more specific pattern wins, so `LumexUI` and `LumexUI.Motion` only restore from `chrismeller`, and everything else from nuget.org.

### 2. Get a token

GitHub Packages needs a token even for public packages. Use a **personal access token (classic) with the `read:packages` scope**. GitHub Packages doesn't accept fine-grained tokens ([GitHub docs](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry#authenticating-to-github-packages)).

### 3. Give NuGet the credentials

The credentials belong to the source named `chrismeller` in the `nuget.config` above. Keep them out of that file.

**Locally:** store them in your user-level NuGet config. That file is `~/.nuget/NuGet/NuGet.Config` on macOS and Linux, and `%APPDATA%\NuGet\NuGet.Config` on Windows:

```shell
dotnet nuget add source "https://nuget.pkg.github.com/chrismeller/index.json" \
  --name chrismeller --username <github-user> --password <token> \
  --store-password-in-clear-text --configfile ~/.nuget/NuGet/NuGet.Config
```

NuGet only encrypts stored passwords on Windows, which is why `--store-password-in-clear-text` is needed on macOS and Linux. On Windows you can leave it out.

**In CI:** set `NuGetPackageSourceCredentials_chrismeller`, which NuGet reads for the `chrismeller` source. In GitHub Actions:

```yaml
env:
  NuGetPackageSourceCredentials_chrismeller: Username=${{ github.actor }};Password=${{ secrets.GITHUB_TOKEN }}
```

Use `secrets.GITHUB_TOKEN` with `permissions: packages: read`, and only if the package's settings give your repository Actions access ("Manage Actions access" on the package page). Otherwise, store a classic token with `read:packages` as a secret and use that.

### 4. Add the package

```shell
dotnet add package LumexUI
```

## Documentation

The fork doesn't host a documentation site. Upstream's site, [lumexui.org](https://lumexui.org), covers setup and the components as of 2.4.0. The same docs site is in `docs/` and runs locally with `dotnet run --project docs/LumexUI.Docs`.

## Building

`mise install` sets up the pinned .NET SDK, Node and pnpm (see `mise.toml`). The build needs Node and pnpm because rollup generates `popover.bundle.js` during it.

```shell
dotnet build
dotnet test tests/LumexUI.Tests
```

Restoring needs the GitHub Packages credentials from [step 3](#3-give-nuget-the-credentials), because `LumexUI.Motion` comes from that feed.

## Contributing

Issues and pull requests are welcome on [this repository](https://github.com/chrismeller/lumexui/issues).

Please adhere to this project's [Code of Conduct](https://github.com/chrismeller/lumexui/blob/main/CODE_OF_CONDUCT.md).

## License

LumexUI is licensed under the terms of the [MIT license](https://github.com/chrismeller/lumexui/blob/main/LICENSE).
