# VS²Sync - Open in Other IDE

[![Build Status](https://img.shields.io/github/actions/workflow/status/werddomain/VSSync/build.yml?branch=main&style=flat-square&label=build)](https://github.com/werddomain/VSSync/actions/workflows/build.yml)
[![License](https://img.shields.io/github/license/werddomain/VSSync?style=flat-square)](https://github.com/werddomain/VSSync/blob/main/LICENSE)

Seamlessly switch between **Visual Studio** and **VS Code** while keeping your current file and cursor position synchronized.

## Features

- **Cross-IDE File Opening**: Open the current file in VS Code from Visual Studio with a single click
- **Cursor Position Sync**: Maintains line and column position when switching IDEs
- **Workspace Matching**: Automatically finds the matching VS Code instance with the same folder open
- **Multiple Instance Support**: Handles multiple IDE instances gracefully
- **Window Focus**: Automatically brings VS Code to the foreground

## Usage

### Open Current File in VS Code

1. Open a file in your solution
2. Use one of these methods:
   - **Keyboard Shortcut**: Press `Ctrl+Shift+V`, then `S`
   - **Context Menu**: Right-click on the document tab and select "Open in VS Code"

## Requirements

- **Visual Studio**: 2022 (17.x) or 2026 (18.x)
- **VS Code**: 1.74.0 or later with the [VS²Sync VS Code extension](https://marketplace.visualstudio.com/items?itemName=KazoMedia.vs2sync) installed
- Both IDEs must have the same workspace/solution/folder open

## Troubleshooting

### No VS Code Instance Found

1. Ensure the VS²Sync extension is installed in VS Code
2. Verify VS Code has the same folder open as your Visual Studio solution
3. Check that no firewall is blocking local TCP connections on ports 52342-52442

### Connection Issues

- Check for antivirus software that might be blocking connections
- Ensure both extensions are enabled and running

## Related Extensions

- [VS²Sync for VS Code](https://marketplace.visualstudio.com/items?itemName=KazoMedia.vs2sync) - The companion extension for VS Code

## Links

- [GitHub Repository](https://github.com/werddomain/VSSync)
- [Report Issues](https://github.com/werddomain/VSSync/issues)
- [Changelog](https://github.com/werddomain/VSSync/releases)

## License

MIT License - see [LICENSE](https://github.com/werddomain/VSSync/blob/main/LICENSE) for details.
