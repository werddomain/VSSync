# VSSync - Open in Other IDE

A suite of synchronized extensions for **Visual Studio Code** and **Visual Studio** that allows you to open the current file in the other IDE with a single click.

## Features

- **Cross-IDE File Opening**: Open the current file in VS Code from Visual Studio, or vice versa
- **Cursor Position Sync**: Maintains line and column position when switching IDEs
- **Workspace Matching**: Automatically finds the matching IDE instance with the same workspace/solution open
- **Multiple Instance Support**: Handles multiple IDE instances gracefully, remembering your choice per session
- **Window Focus**: Automatically brings the target IDE to the foreground

## Installation

### VS Code Extension

1. Open VS Code
2. Go to Extensions (Ctrl+Shift+X)
3. Search for "VSSync"
4. Click Install

**Or install from VSIX:**
```bash
cd vscode-extension
npm install
npm run compile
# Package with vsce: vsce package
```

### Visual Studio Extension

1. Open Visual Studio
2. Go to Extensions > Manage Extensions
3. Search for "VSSync"
4. Click Download

**Or build from source:**
1. Open `visual-studio-extension/VSSync/VSSync.csproj` in Visual Studio 2022
2. Build the solution
3. The VSIX will be in `bin/Debug` or `bin/Release`

## Usage

### From VS Code
1. Open a file in your workspace
2. Right-click on the editor tab or title
3. Select "Open in Visual Studio"

### From Visual Studio
1. Open a file in your solution
2. Right-click on the document tab
3. Select "Open in VS Code"

## Requirements

- **VS Code**: 1.74.0 or later
- **Visual Studio**: 2017, 2019, 2022, or later
- Both extensions must be installed
- Both IDEs must have the same workspace/solution/folder open

## Architecture

### IPC Communication

The extensions communicate using TCP sockets with a port discovery mechanism:

- **Base Port**: 52342 (configurable)
- **Port Range**: 100 ports scanned for discovery
- **Protocol**: JSON messages over TCP

### Message Types

| Type | Description |
|------|-------------|
| `DISCOVER` | Find IDE instances with matching workspace |
| `DISCOVER_RESPONSE` | Response with instance information |
| `OPEN_FILE` | Request to open a file |
| `OPEN_FILE_RESPONSE` | Response to open file request |
| `PING` / `PONG` | Keep-alive messages |

### Window Focus

- **Visual Studio**: Uses Win32 API (`SetForegroundWindow`, `BringWindowToTop`, etc.)
- **VS Code**: Uses internal command API

## Configuration

### VS Code Settings

```json
{
  "vssync.ipcPort": 52342,
  "vssync.timeout": 5000
}
```

- `vssync.ipcPort`: Base port for IPC communication (scans port to port+100)
- `vssync.timeout`: Timeout in milliseconds for IPC operations

## Troubleshooting

### No IDE Instance Found

1. Ensure both extensions are installed and enabled
2. Verify both IDEs have the same folder/solution open
3. Check that no firewall is blocking local TCP connections on ports 52342-52442

### File Opens But Window Doesn't Focus

This can happen due to Windows focus stealing prevention. Try:
1. Click on the taskbar icon of the target IDE
2. Some applications may prevent window focus stealing

### Connection Timeout

1. Increase the timeout setting in VS Code
2. Check for antivirus software that might be blocking connections

## Development

### Project Structure

```
VSSync/
├── vscode-extension/          # VS Code extension (TypeScript)
│   ├── src/
│   │   ├── extension.ts       # Main extension entry
│   │   ├── ipcClient.ts       # IPC client for VS communication
│   │   ├── ipcServer.ts       # IPC server for incoming requests
│   │   └── protocol.ts        # Protocol type definitions
│   ├── package.json
│   └── tsconfig.json
├── visual-studio-extension/   # Visual Studio extension (C#)
│   └── VSSync/
│       ├── VSSyncPackage.cs   # Main package
│       ├── IpcServer.cs       # IPC server
│       ├── IpcClient.cs       # IPC client
│       ├── Protocol.cs        # Protocol types
│       ├── WindowHelper.cs    # Win32 window management
│       ├── OpenInVSCodeCommand.cs
│       └── VSSync.csproj
└── shared/
    └── PROTOCOL.md            # Protocol specification
```

### Building

**VS Code Extension:**
```bash
cd vscode-extension
npm install
npm run compile
```

**Visual Studio Extension:**
```bash
cd visual-studio-extension/VSSync
dotnet build
```

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Credits

Created by [Benoit Robin](https://github.com/werddomain)
