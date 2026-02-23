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

Publish a self-contained build:

```
dotnet publish src/portforwarder.csproj -c Release -r win-x64 --self-contained
```

Install the service (from an elevated prompt):

```
sc.exe create PortForwarder binPath="C:\path\to\publish\portforwarder.exe"
sc.exe start PortForwarder
```

Uninstall:

```
sc.exe stop PortForwarder
sc.exe delete PortForwarder
```

When running as a service, configuration is read from `appsettings.json` next to the executable.

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
