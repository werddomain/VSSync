/**
 * VSSync IPC Client
 * Handles TCP communication for discovering and communicating with Visual Studio instances
 */

import * as net from 'net';
import * as path from 'path';
import {
    IdeInstance,
    createMessage,
    parseMessage,
    isDiscoverResponsePayload,
    isOpenFileResponsePayload
} from './protocol';

const DEFAULT_BASE_PORT = 52342;
const PORT_RANGE = 100;
const CONNECTION_TIMEOUT = 1000;
const OPERATION_TIMEOUT = 5000;

export class IpcClient {
    private basePort: number;
    private timeout: number;

    constructor(basePort: number = DEFAULT_BASE_PORT, timeout: number = OPERATION_TIMEOUT) {
        this.basePort = basePort;
        this.timeout = timeout;
    }

    /**
     * Discover all Visual Studio instances that have a matching workspace
     */
    async discoverInstances(workspacePath: string): Promise<IdeInstance[]> {
        const instances: IdeInstance[] = [];
        const normalizedPath = this.normalizePath(workspacePath);
        const promises: Promise<IdeInstance | null>[] = [];

        for (let port = this.basePort; port < this.basePort + PORT_RANGE; port++) {
            promises.push(this.probePort(port, normalizedPath));
        }

        const results = await Promise.allSettled(promises);
        
        for (const result of results) {
            if (result.status === 'fulfilled' && result.value) {
                instances.push(result.value);
            }
        }

        return instances;
    }

    /**
     * Discover all active Visual Studio instances regardless of workspace
     */
    async discoverAllInstances(): Promise<IdeInstance[]> {
        const instances: IdeInstance[] = [];
        const promises: Promise<IdeInstance | null>[] = [];

        for (let port = this.basePort; port < this.basePort + PORT_RANGE; port++) {
            promises.push(this.probePortAllInstances(port));
        }

        const results = await Promise.allSettled(promises);
        
        for (const result of results) {
            if (result.status === 'fulfilled' && result.value) {
                instances.push(result.value);
            }
        }

        return instances;
    }

    /**
     * Send an open file request to a specific IDE instance
     */
    async openFile(
        instance: IdeInstance,
        filePath: string,
        line?: number,
        column?: number
    ): Promise<boolean> {
        return new Promise((resolve) => {
            const socket = new net.Socket();
            let responseReceived = false;

            const timeoutId = setTimeout(() => {
                if (!responseReceived) {
                    socket.destroy();
                    resolve(false);
                }
            }, this.timeout);

            socket.connect(instance.port, '127.0.0.1', () => {
                const payload: Record<string, unknown> = {
                    filePath: this.normalizePath(filePath),
                    line,
                    column,
                    focus: true
                };
                const message = createMessage('OPEN_FILE', payload, 'vscode');

                socket.write(JSON.stringify(message) + '\n');
            });

            let buffer = '';
            socket.on('data', (data) => {
                buffer += data.toString();
                const lines = buffer.split('\n');
                
                for (let i = 0; i < lines.length - 1; i++) {
                    const msg = parseMessage(lines[i]);
                    if (msg && msg.type === 'OPEN_FILE_RESPONSE') {
                        responseReceived = true;
                        clearTimeout(timeoutId);
                        socket.destroy();
                        
                        if (isOpenFileResponsePayload(msg.payload)) {
                            resolve(msg.payload.success);
                        } else {
                            resolve(false);
                        }
                        return;
                    }
                }
                buffer = lines[lines.length - 1];
            });

            socket.on('error', () => {
                clearTimeout(timeoutId);
                socket.destroy();
                resolve(false);
            });

            socket.on('close', () => {
                clearTimeout(timeoutId);
                if (!responseReceived) {
                    resolve(false);
                }
            });
        });
    }

    /**
     * Probe a specific port for a Visual Studio instance
     */
    private probePort(port: number, workspacePath: string): Promise<IdeInstance | null> {
        return new Promise((resolve) => {
            const socket = new net.Socket();
            let responseReceived = false;

            const timeoutId = setTimeout(() => {
                socket.destroy();
                resolve(null);
            }, CONNECTION_TIMEOUT);

            socket.connect(port, '127.0.0.1', () => {
                const payload: Record<string, unknown> = {
                    workspacePath
                };
                const message = createMessage('DISCOVER', payload, 'vscode');

                socket.write(JSON.stringify(message) + '\n');
            });

            let buffer = '';
            socket.on('data', (data) => {
                buffer += data.toString();
                const lines = buffer.split('\n');
                
                for (let i = 0; i < lines.length - 1; i++) {
                    const msg = parseMessage(lines[i]);
                    if (msg && msg.type === 'DISCOVER_RESPONSE') {
                        responseReceived = true;
                        clearTimeout(timeoutId);
                        
                        if (!isDiscoverResponsePayload(msg.payload)) {
                            socket.destroy();
                            resolve(null);
                            return;
                        }
                        
                        const payload = msg.payload;
                        
                        // Only return if it's a Visual Studio instance with matching workspace
                        if (payload.ide === 'visualstudio' && 
                            this.pathsMatch(payload.workspacePath, workspacePath)) {
                            socket.destroy();
                            resolve({
                                port: payload.port,
                                ide: payload.ide,
                                version: payload.version,
                                workspacePath: payload.workspacePath,
                                solutionPath: payload.solutionPath,
                                pid: payload.pid,
                                windowHandle: payload.windowHandle
                            });
                            return;
                        }
                        socket.destroy();
                        resolve(null);
                    }
                }
                buffer = lines[lines.length - 1];
            });

            socket.on('error', () => {
                clearTimeout(timeoutId);
                socket.destroy();
                resolve(null);
            });

            socket.on('close', () => {
                clearTimeout(timeoutId);
                if (!responseReceived) {
                    resolve(null);
                }
            });
        });
    }

    /**
     * Probe a specific port for any Visual Studio instance (regardless of workspace)
     */
    private probePortAllInstances(port: number): Promise<IdeInstance | null> {
        return new Promise((resolve) => {
            const socket = new net.Socket();
            let responseReceived = false;

            const timeoutId = setTimeout(() => {
                socket.destroy();
                resolve(null);
            }, CONNECTION_TIMEOUT);

            socket.connect(port, '127.0.0.1', () => {
                const payload: Record<string, unknown> = {
                    workspacePath: ''
                };
                const message = createMessage('DISCOVER', payload, 'vscode');

                socket.write(JSON.stringify(message) + '\n');
            });

            let buffer = '';
            socket.on('data', (data) => {
                buffer += data.toString();
                const lines = buffer.split('\n');
                
                for (let i = 0; i < lines.length - 1; i++) {
                    const msg = parseMessage(lines[i]);
                    if (msg && msg.type === 'DISCOVER_RESPONSE') {
                        responseReceived = true;
                        clearTimeout(timeoutId);
                        
                        if (!isDiscoverResponsePayload(msg.payload)) {
                            socket.destroy();
                            resolve(null);
                            return;
                        }
                        
                        const payload = msg.payload;
                        
                        // Return any Visual Studio instance
                        if (payload.ide === 'visualstudio') {
                            socket.destroy();
                            resolve({
                                port: payload.port,
                                ide: payload.ide,
                                version: payload.version,
                                workspacePath: payload.workspacePath,
                                solutionPath: payload.solutionPath,
                                pid: payload.pid,
                                windowHandle: payload.windowHandle
                            });
                            return;
                        }
                        socket.destroy();
                        resolve(null);
                    }
                }
                buffer = lines[lines.length - 1];
            });

            socket.on('error', () => {
                clearTimeout(timeoutId);
                socket.destroy();
                resolve(null);
            });

            socket.on('close', () => {
                clearTimeout(timeoutId);
                if (!responseReceived) {
                    resolve(null);
                }
            });
        });
    }

    /**
     * Normalize file path for cross-platform comparison.
     * Handles various path formats including UNC paths and removes trailing separators.
     */
    private normalizePath(filePath: string): string {
        // Handle empty paths
        if (!filePath) {
            return '';
        }
        
        // Normalize path separators and resolve .. and .
        let normalized = path.normalize(filePath);
        
        // Convert backslashes to forward slashes for consistency
        normalized = normalized.replace(/\\/g, '/');
        
        // Remove trailing separator (unless it's a root path like "/" or "C:/")
        if (normalized.length > 1 && normalized.endsWith('/')) {
            const withoutTrailing = normalized.slice(0, -1);
            // Keep trailing slash only for root paths like "C:"
            if (withoutTrailing.length !== 2 || withoutTrailing[1] !== ':') {
                normalized = withoutTrailing;
            }
        }
        
        return normalized.toLowerCase();
    }

    /**
     * Check if two paths match (considering workspace subdirectories)
     */
    private pathsMatch(path1: string, path2: string): boolean {
        const normalized1 = this.normalizePath(path1);
        const normalized2 = this.normalizePath(path2);
        
        return normalized1.startsWith(normalized2) || 
               normalized2.startsWith(normalized1) ||
               normalized1 === normalized2;
    }
}
