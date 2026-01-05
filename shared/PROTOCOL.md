# VSSync IPC Protocol Specification

## Overview
The VSSync protocol enables communication between Visual Studio Code and Visual Studio instances running on the same machine. It uses TCP sockets with a port discovery mechanism.

## Port Discovery
- Base port: 52342 (configurable)
- Port range: base to base+100
- Each IDE instance listens on a free port in this range
- Discovery works by broadcasting a DISCOVER message to all ports in range

## Message Format
All messages are JSON encoded with a newline delimiter.

### Message Structure
```json
{
  "type": "REQUEST_TYPE",
  "payload": { ... },
  "timestamp": 1704498423000,
  "sourceIde": "vscode|visualstudio",
  "sourcePid": 12345
}
```

## Message Types

### DISCOVER
Sent to find active IDE instances.
```json
{
  "type": "DISCOVER",
  "payload": {
    "workspacePath": "C:/Projects/MyProject"
  }
}
```

### DISCOVER_RESPONSE
Response to DISCOVER message.
```json
{
  "type": "DISCOVER_RESPONSE",
  "payload": {
    "port": 52345,
    "ide": "visualstudio|vscode",
    "version": "17.8.0",
    "workspacePath": "C:/Projects/MyProject",
    "solutionPath": "C:/Projects/MyProject/MySolution.sln",
    "pid": 12345,
    "windowHandle": 123456
  }
}
```

### OPEN_FILE
Request to open a file in the target IDE.
```json
{
  "type": "OPEN_FILE",
  "payload": {
    "filePath": "C:/Projects/MyProject/src/main.cs",
    "line": 42,
    "column": 10,
    "focus": true
  }
}
```

### OPEN_FILE_RESPONSE
Response to OPEN_FILE request.
```json
{
  "type": "OPEN_FILE_RESPONSE",
  "payload": {
    "success": true,
    "error": null
  }
}
```

### PING
Keep-alive message.
```json
{
  "type": "PING",
  "payload": {}
}
```

### PONG
Response to PING.
```json
{
  "type": "PONG",
  "payload": {}
}
```

## Connection Flow

1. On IDE startup, start TCP server on first available port in range
2. When "Open in Other IDE" is triggered:
   a. Get current file path and cursor position
   b. Send DISCOVER to all ports in range
   c. Collect responses that match workspace/solution
   d. If multiple matches, prompt user to select
   e. Send OPEN_FILE to selected instance
   f. Target instance opens file and focuses window
3. On connection close, cleanup resources

## Error Handling
- Connection timeout: 1000ms per port
- Total operation timeout: 5000ms
- Retry on transient failures: 3 attempts
