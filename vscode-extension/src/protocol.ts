/**
 * VSSync IPC Protocol Types
 * Shared type definitions for communication between VS Code and Visual Studio
 */

export type IdeType = 'vscode' | 'visualstudio';

export type MessageType = 
    | 'DISCOVER'
    | 'DISCOVER_RESPONSE'
    | 'OPEN_FILE'
    | 'OPEN_FILE_RESPONSE'
    | 'PING'
    | 'PONG';

export interface IpcMessage {
    type: MessageType;
    payload: Record<string, unknown>;
    timestamp: number;
    sourceIde: IdeType;
    sourcePid: number;
}

export interface DiscoverPayload {
    workspacePath: string;
}

export interface DiscoverResponsePayload {
    port: number;
    ide: IdeType;
    version: string;
    workspacePath: string;
    solutionPath?: string;
    pid: number;
    windowHandle?: number;
}

export interface OpenFilePayload {
    filePath: string;
    line?: number;
    column?: number;
    focus: boolean;
}

export interface OpenFileResponsePayload {
    success: boolean;
    error?: string;
}

export interface IdeInstance {
    port: number;
    ide: IdeType;
    version: string;
    workspacePath: string;
    solutionPath?: string;
    pid: number;
    windowHandle?: number;
}

export function createMessage(
    type: MessageType,
    payload: Record<string, unknown>,
    sourceIde: IdeType
): IpcMessage {
    return {
        type,
        payload,
        timestamp: Date.now(),
        sourceIde,
        sourcePid: process.pid
    };
}

export function parseMessage(data: string): IpcMessage | null {
    try {
        const parsed = JSON.parse(data);
        if (parsed && typeof parsed.type === 'string' && parsed.payload) {
            return parsed as IpcMessage;
        }
        return null;
    } catch {
        return null;
    }
}
