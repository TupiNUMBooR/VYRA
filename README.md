# VYRA

<img src="VYRA.WPF/Assets/VYRA3.png" height="400">

![Release](https://github.com/TupiNUMBooR/VYRA/actions/workflows/cicd.yml/badge.svg)
![Latest Release](https://img.shields.io/github/release/TupiNUMBooR/VYRA)
![Release Date](https://img.shields.io/github/release-date/TupiNUMBooR/VYRA)

![Top Lang](https://img.shields.io/github/languages/top/TupiNUMBooR/VYRA?logo=csharp)
![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![WPF](https://img.shields.io/badge/UI-WPF-blue)
![itch.io](https://img.shields.io/badge/deploy-itch.io-blue?logo=itchdotio)

VYRA is a minimalist desktop overlay for screenshots, notes, and quick AI context.

[VYRA builds on itch.io](https://tupinumboor.itch.io/vyra)

## Development

VYRA is written in C# and WPF.

Requirements for building or modifying VYRA:

* Windows
* .NET 8 SDK
  `winget install Microsoft.DotNet.SDK.8`

### Run

`dotnet run --project VYRA.WPF`

### Build portable exe locally

`dotnet publish -c Release`

Output:

`VYRA.WPF/bin/Release/net8.0-windows/win-x64/publish/VYRA.WPF.exe`

### Release setup

Releases are created automatically when a version tag is pushed.

Tag format:

`*.*.*`

Example:

`0.1.0`

Required repository secret:

| Name             | Description                    |
| ---------------- | ------------------------------ |
| `BUTLER_API_KEY` | itch.io API key used by butler |

Create it here:

[GitHub Actions secrets](https://github.com/tupinumboor/VYRA/settings/secrets/actions)

The release workflow uploads the Windows build to:

`tupinumboor/vyra:windows`

### Publish

`git tag 0.1.0`

`git push origin 0.1.0`
