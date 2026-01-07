# VS²Sync - Open in Other IDE

[![Build Status](https://img.shields.io/github/actions/workflow/status/werddomain/VSSync/build.yml?branch=main&style=flat-square&label=build)](https://github.com/werddomain/VSSync/actions/workflows/build.yml)
[![License](https://img.shields.io/github/license/werddomain/VSSync?style=flat-square)](https://github.com/werddomain/VSSync/blob/main/LICENSE)

Seamlessly switch between **VS Code** and **Visual Studio** while keeping your current file and cursor position synchronized.

## Features

- **Cross-IDE File Opening**: Open the current file in Visual Studio from VS Code with a single click
- **Cursor Position Sync**: Maintains line and column position when switching IDEs
- **Workspace Matching**: Automatically finds the matching Visual Studio instance with the same solution open
- **Multiple Instance Support**: Handles multiple IDE instances gracefully
- **Window Focus**: Automatically brings Visual Studio to the foreground

## Usage

### Open Current File in Visual Studio

1. Open a file in your workspace
2. Use one of these methods:
   - **Keyboard Shortcut**: Press `Ctrl+Shift+V`, then `S`
   - **Context Menu**: Right-click on the editor tab and select "Open in Visual Studio"
   - **Command Palette**: Press `Ctrl+Shift+P` and type "Open in Visual Studio"

## Requirements

- **VS Code**: 1.74.0 or later
- **Visual Studio**: 2022 or 2026 with the [VS²Sync Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=KazoMedia.VS2Sync-VisualStudio) installed
- Both IDEs must have the same workspace/solution/folder open

## Extension Settings

This extension contributes the following settings:

* `vs2sync.ipcPort`: Base port for IPC communication (default: 52342)
* `vs2sync.timeout`: Timeout in milliseconds for IPC operations (default: 5000)

## Troubleshooting

### No Visual Studio Instance Found

1. Ensure the VS²Sync extension is installed in Visual Studio
2. Verify Visual Studio has the same folder/solution open
3. Check that no firewall is blocking local TCP connections on ports 52342-52442

### Connection Timeout

1. Increase the timeout setting: `vs2sync.timeout`
2. Check for antivirus software that might be blocking connections

## Related Extensions

- [VS²Sync for Visual Studio](https://marketplace.visualstudio.com/items?itemName=KazoMedia.VS2Sync-VisualStudio) - The companion extension for Visual Studio

## Links

- [GitHub Repository](https://github.com/werddomain/VSSync)
- [Report Issues](https://github.com/werddomain/VSSync/issues)
- [Changelog](https://github.com/werddomain/VSSync/releases)

## License

MIT License - see [LICENSE](https://github.com/werddomain/VSSync/blob/main/LICENSE) for details.
