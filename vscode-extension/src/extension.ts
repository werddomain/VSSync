/**
 * VS²Sync - VS Code Extension
 * Main extension entry point
 */

import * as vscode from 'vscode';
import { IpcClient } from './ipcClient';
import { IpcServer } from './ipcServer';
import { IdeInstance } from './protocol';

let ipcServer: IpcServer | null = null;
let ipcClient: IpcClient | null = null;

// Session storage for remembering user's instance choice
const instanceChoiceCache = new Map<string, IdeInstance>();

export async function activate(context: vscode.ExtensionContext): Promise<void> {
    console.log('VS²Sync extension is now active');

    // Get configuration
    const config = vscode.workspace.getConfiguration('vs2sync');
    const basePort = config.get<number>('ipcPort', 52342);
    const timeout = config.get<number>('timeout', 5000);

    // Initialize IPC client
    ipcClient = new IpcClient(basePort, timeout);

    // Start IPC server
    ipcServer = new IpcServer(basePort);
    try {
        const port = await ipcServer.start();
        console.log(`VS²Sync IPC server started on port ${port}`);
    } catch (error) {
        console.error('Failed to start VS²Sync IPC server:', error);
        vscode.window.showWarningMessage(
            'VS²Sync: Failed to start IPC server. Incoming requests from Visual Studio will not work.'
        );
    }

    // Register the "Open in Visual Studio" command
    const openInVsCommand = vscode.commands.registerCommand(
        'vs2sync.openInVisualStudio',
        async (uri?: vscode.Uri) => {
            await openInVisualStudio(uri);
        }
    );

    context.subscriptions.push(openInVsCommand);

    // Clean up on deactivation
    context.subscriptions.push({
        dispose: () => {
            if (ipcServer) {
                ipcServer.stop();
            }
        }
    });
}

/**
 * Open the current file in Visual Studio
 */
async function openInVisualStudio(uri?: vscode.Uri): Promise<void> {
    // Get the file to open
    let filePath: string;
    let line: number | undefined;
    let column: number | undefined;

    if (uri) {
        filePath = uri.fsPath;
    } else {
        const editor = vscode.window.activeTextEditor;
        if (!editor) {
            vscode.window.showWarningMessage('VS²Sync: No active file to open');
            return;
        }
        filePath = editor.document.uri.fsPath;
        line = editor.selection.active.line + 1; // Convert to 1-based
        column = editor.selection.active.character + 1;
    }

    // Get the workspace path
    const workspaceFolders = vscode.workspace.workspaceFolders;
    if (!workspaceFolders || workspaceFolders.length === 0) {
        vscode.window.showWarningMessage('VS²Sync: No workspace folder open');
        return;
    }

    const workspacePath = workspaceFolders[0].uri.fsPath;

    // Show progress while discovering instances
    await vscode.window.withProgress(
        {
            location: vscode.ProgressLocation.Notification,
            title: 'VS²Sync: Searching for Visual Studio instances...',
            cancellable: true
        },
        async (progress, token) => {
            try {
                // Discover Visual Studio instances
                const instances = await ipcClient!.discoverInstances(workspacePath);

                if (token.isCancellationRequested) {
                    return;
                }

                if (instances.length === 0) {
                    // No matching instances found, try to discover all active instances
                    const allInstances = await ipcClient!.discoverAllInstances();
                    
                    if (allInstances.length === 0) {
                        vscode.window.showWarningMessage(
                            'VS²Sync: No Visual Studio instance found. ' +
                            'Make sure Visual Studio has the VS²Sync extension installed.'
                        );
                    } else {
                        // Show available instances to help the user
                        const instanceList = allInstances.map((inst, idx) => 
                            `${idx + 1}. Visual Studio ${inst.version}\n   Path: ${inst.solutionPath || inst.workspacePath || 'No solution open'}\n   PID: ${inst.pid}`
                        ).join('\n');
                        
                        const openAnyway = await vscode.window.showWarningMessage(
                            `VS²Sync: No Visual Studio instance found with the same workspace open.\n\n` +
                            `Active Visual Studio instances (${allInstances.length}):\n${instanceList}\n\n` +
                            `Click "Open in First Instance" to open in the first instance, or "Cancel" to abort.`,
                            { modal: true },
                            'Open in First Instance',
                            'Cancel'
                        );
                        
                        if (openAnyway === 'Open in First Instance' && allInstances.length > 0) {
                            progress.report({ message: 'Opening file in Visual Studio...' });
                            const success = await ipcClient!.openFile(allInstances[0], filePath, line, column);
                            if (success) {
                                vscode.window.setStatusBarMessage('VS²Sync: File opened in Visual Studio', 3000);
                            } else {
                                vscode.window.showErrorMessage('VS²Sync: Failed to open file in Visual Studio');
                            }
                        }
                    }
                    return;
                }

                // Select the instance to use
                let selectedInstance: IdeInstance;

                if (instances.length === 1) {
                    selectedInstance = instances[0];
                } else {
                    // Check if we have a cached choice for this workspace
                    const cachedChoice = instanceChoiceCache.get(workspacePath);
                    if (cachedChoice && instances.some(i => i.pid === cachedChoice.pid)) {
                        selectedInstance = cachedChoice;
                    } else {
                        // Let user choose
                        const items = instances.map(instance => ({
                            label: `Visual Studio ${instance.version}`,
                            description: instance.solutionPath || instance.workspacePath,
                            detail: `PID: ${instance.pid}`,
                            instance
                        }));

                        const selected = await vscode.window.showQuickPick(items, {
                            placeHolder: 'Multiple Visual Studio instances found. Select one:',
                            title: 'VS²Sync - Select Target Instance'
                        });

                        if (!selected) {
                            return; // User cancelled
                        }

                        selectedInstance = selected.instance;
                        // Cache the choice
                        instanceChoiceCache.set(workspacePath, selectedInstance);
                    }
                }

                progress.report({ message: 'Opening file in Visual Studio...' });

                // Open the file
                const success = await ipcClient!.openFile(
                    selectedInstance,
                    filePath,
                    line,
                    column
                );

                if (success) {
                    // Show brief notification
                    vscode.window.setStatusBarMessage('VS²Sync: File opened in Visual Studio', 3000);
                } else {
                    vscode.window.showErrorMessage(
                        'VS²Sync: Failed to open file in Visual Studio'
                    );
                }
            } catch (error) {
                const message = error instanceof Error ? error.message : 'Unknown error';
                vscode.window.showErrorMessage(`VS²Sync: Error - ${message}`);
            }
        }
    );
}

export function deactivate(): void {
    if (ipcServer) {
        ipcServer.stop();
        ipcServer = null;
    }
    ipcClient = null;
    instanceChoiceCache.clear();
}
