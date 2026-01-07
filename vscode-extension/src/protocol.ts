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
        // Remove BOM (Byte Order Mark) and trim whitespace/control characters
        const cleanedData = data.replace(/^\uFEFF/, '').trim();
        const parsed = JSON.parse(cleanedData);
        if (parsed && typeof parsed.type === 'string' && parsed.payload) {
            return parsed as IpcMessage;
        }
        return null;
    } catch {
        return null;
    }
}

/**
 * Type guard for DiscoverPayload
 */
export function isDiscoverPayload(payload: unknown): payload is DiscoverPayload {
    if (typeof payload !== 'object' || payload === null) {
        return false;
    }
    const p = payload as Record<string, unknown>;
    return typeof p.workspacePath === 'string';
}

/**
 * Type guard for DiscoverResponsePayload
 */
export function isDiscoverResponsePayload(payload: unknown): payload is DiscoverResponsePayload {
    if (typeof payload !== 'object' || payload === null) {
        return false;
    }
    const p = payload as Record<string, unknown>;
    return typeof p.port === 'number' &&
           (p.ide === 'vscode' || p.ide === 'visualstudio') &&
           typeof p.version === 'string' &&
           typeof p.workspacePath === 'string' &&
           typeof p.pid === 'number';
}

/**
 * Type guard for OpenFilePayload
 */
export function isOpenFilePayload(payload: unknown): payload is OpenFilePayload {
    if (typeof payload !== 'object' || payload === null) {
        return false;
    }
    const p = payload as Record<string, unknown>;
    return typeof p.filePath === 'string' &&
           (p.line === undefined || typeof p.line === 'number') &&
           (p.column === undefined || typeof p.column === 'number') &&
           typeof p.focus === 'boolean';
}

/**
 * Type guard for OpenFileResponsePayload
 */
export function isOpenFileResponsePayload(payload: unknown): payload is OpenFileResponsePayload {
    if (typeof payload !== 'object' || payload === null) {
        return false;
    }
    const p = payload as Record<string, unknown>;
    return typeof p.success === 'boolean';
}
