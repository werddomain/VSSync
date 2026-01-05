/**
 * VSSync IPC Server
 * TCP server that listens for requests from Visual Studio
 */

import * as net from 'net';
import * as vscode from 'vscode';
import {
    IpcMessage,
    createMessage,
    parseMessage,
    DiscoverPayload,
    OpenFilePayload
} from './protocol';

const DEFAULT_BASE_PORT = 52342;
const PORT_RANGE = 100;

export class IpcServer {
    private server: net.Server | null = null;
    private port: number = 0;
    private basePort: number;

    constructor(basePort: number = DEFAULT_BASE_PORT) {
        this.basePort = basePort;
    }

    /**
     * Start the IPC server on the first available port
     */
    async start(): Promise<number> {
        return new Promise((resolve, reject) => {
            this.server = net.createServer((socket) => {
                this.handleConnection(socket);
            });

            this.tryListen(this.basePort, resolve, reject);
        });
    }

    /**
     * Try to listen on a port, incrementing if busy
     */
    private tryListen(port: number, resolve: (port: number) => void, reject: (err: Error) => void): void {
        if (port >= this.basePort + PORT_RANGE) {
            reject(new Error('No available port found in range'));
            return;
        }

        this.server!.once('error', (err: NodeJS.ErrnoException) => {
            if (err.code === 'EADDRINUSE') {
                this.tryListen(port + 1, resolve, reject);
            } else {
                reject(err);
            }
        });

        this.server!.listen(port, '127.0.0.1', () => {
            this.port = port;
            console.log(`VSSync IPC server listening on port ${port}`);
            resolve(port);
        });
    }

    /**
     * Handle incoming connection
     */
    private handleConnection(socket: net.Socket): void {
        let buffer = '';

        socket.on('data', async (data) => {
            buffer += data.toString();
            const lines = buffer.split('\n');
            
            for (let i = 0; i < lines.length - 1; i++) {
                const message = parseMessage(lines[i]);
                if (message) {
                    await this.handleMessage(socket, message);
                }
            }
            buffer = lines[lines.length - 1];
        });

        socket.on('error', (err) => {
            console.error('VSSync IPC connection error:', err.message);
        });
    }

    /**
     * Handle incoming IPC message
     */
    private async handleMessage(socket: net.Socket, message: IpcMessage): Promise<void> {
        switch (message.type) {
            case 'DISCOVER':
                await this.handleDiscover(socket, message.payload as unknown as DiscoverPayload);
                break;
            case 'OPEN_FILE':
                await this.handleOpenFile(socket, message.payload as unknown as OpenFilePayload);
                break;
            case 'PING':
                this.sendResponse(socket, 'PONG', {});
                break;
        }
    }

    /**
     * Handle DISCOVER request
     */
    private async handleDiscover(socket: net.Socket, _payload: DiscoverPayload): Promise<void> {
        const workspaceFolders = vscode.workspace.workspaceFolders;
        const workspacePath = workspaceFolders && workspaceFolders.length > 0 
            ? workspaceFolders[0].uri.fsPath 
            : '';

        this.sendResponse(socket, 'DISCOVER_RESPONSE', {
            port: this.port,
            ide: 'vscode',
            version: vscode.version,
            workspacePath,
            pid: process.pid
        });
    }

    /**
     * Handle OPEN_FILE request
     */
    private async handleOpenFile(socket: net.Socket, payload: OpenFilePayload): Promise<void> {
        try {
            const uri = vscode.Uri.file(payload.filePath);
            const document = await vscode.workspace.openTextDocument(uri);
            const editor = await vscode.window.showTextDocument(document, {
                preview: false,
                preserveFocus: false
            });

            if (payload.line !== undefined && payload.line > 0) {
                const line = payload.line - 1; // Convert to 0-based
                const column = (payload.column ?? 1) - 1;
                const position = new vscode.Position(line, column);
                editor.selection = new vscode.Selection(position, position);
                editor.revealRange(
                    new vscode.Range(position, position),
                    vscode.TextEditorRevealType.InCenter
                );
            }

            // Try to focus VS Code window
            if (payload.focus) {
                await this.focusWindow();
            }

            this.sendResponse(socket, 'OPEN_FILE_RESPONSE', {
                success: true
            });
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Unknown error';
            this.sendResponse(socket, 'OPEN_FILE_RESPONSE', {
                success: false,
                error: errorMessage
            });
        }
    }

    /**
     * Attempt to focus the VS Code window
     */
    private async focusWindow(): Promise<void> {
        // VS Code doesn't have direct window focus API, but we can try to activate a command
        // that will bring the window to focus
        try {
            await vscode.commands.executeCommand('workbench.action.focusActiveEditorGroup');
        } catch {
            // Ignore focus errors
        }
    }

    /**
     * Send response message
     */
    private sendResponse(socket: net.Socket, type: string, payload: Record<string, unknown>): void {
        const response = createMessage(type as never, payload, 'vscode');
        socket.write(JSON.stringify(response) + '\n');
    }

    /**
     * Stop the IPC server
     */
    stop(): void {
        if (this.server) {
            this.server.close();
            this.server = null;
            this.port = 0;
        }
    }

    /**
     * Get the current server port
     */
    getPort(): number {
        return this.port;
    }
}
