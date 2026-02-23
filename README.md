# SimplePortForwarder

A lightweight TCP port forwarder for Windows. Forwards traffic from a local port to a remote host and port. Can run as a console application or as a Windows Service.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)

## Build

```
dotnet build portforwarder.sln
```

## Run as Console Application

### Using command-line arguments

```
dotnet run --project src/portforwarder.csproj -- --PortForwarder:LocalPort=3390 --PortForwarder:RemoteHost=192.168.15.7 --PortForwarder:RemotePort=3389
```

### Using appsettings.json

Edit `src/appsettings.json` with your desired configuration:

```json
{
  "PortForwarder": {
    "LocalPort": 3390,
    "RemoteHost": "192.168.15.7",
    "RemotePort": 3389
  }
}
```

Then run:

```
dotnet run --project src/portforwarder.csproj
```

Press `Ctrl+C` to stop.

## Install as a Windows Service

### Using the MSI installer

Download the latest `PortForwarder.Installer.msi` from [Releases](https://github.com/michaelkc/SimplePortForwarder/releases) and run it. The MSI installs the application to Program Files and registers the Windows Service.

Start the service after installation:

```
sc.exe start PortForwarder
```

To uninstall, use **Add/Remove Programs** or run the MSI again.

### Building the MSI yourself

```
dotnet publish src/portforwarder.csproj -c Release -r win-x64 --self-contained -o publish
dotnet build installer/PortForwarder.Installer.wixproj -p:PublishDir=%cd%\publish\
```

The MSI is produced at `installer/bin/Debug/PortForwarder.Installer.msi`.

When running as a service, edit `appsettings.json` in the install directory (`C:\Program Files\SimplePortForwarder\`).

## Configuration

| Setting | Description | Example |
|---------|-------------|---------|
| `PortForwarder:LocalPort` | Local port to listen on | `3390` |
| `PortForwarder:RemoteHost` | Remote host to forward to | `192.168.15.7` |
| `PortForwarder:RemotePort` | Remote port to forward to | `3389` |

Configuration can be provided via:
- `appsettings.json`
- Command-line arguments (e.g., `--PortForwarder:LocalPort=3390`)
- Environment variables (e.g., `PortForwarder__LocalPort=3390`)

## Tests

```
dotnet test portforwarder.sln
```

## License

See [LICENSE](LICENSE).
