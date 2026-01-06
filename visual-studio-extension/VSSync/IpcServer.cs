using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VSSync
{
    /// <summary>
    /// IPC Server for receiving requests from VS Code
    /// </summary>
    public class IpcServer
    {
        private const int BasePort = 52342;
        private const int PortRange = 100;

        private readonly VSSyncPackage _package;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private int _port;

        public IpcServer(VSSyncPackage package)
        {
            _package = package;
        }

        public int Port => _port;

        /// <summary>
        /// Start the IPC server
        /// </summary>
        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();

            // Find an available port
            for (int port = BasePort; port < BasePort + PortRange; port++)
            {
                try
                {
                    _listener = new TcpListener(IPAddress.Loopback, port);
                    _listener.Start();
                    _port = port;
                    Debug.WriteLine($"VS²Sync IPC server started on port {port}");
                    break;
                }
                catch (SocketException)
                {
                    continue;
                }
            }

            if (_listener == null)
            {
                Debug.WriteLine("VS²Sync: Failed to start IPC server - no available port");
                return;
            }

            // Start accepting connections in background
            _ = AcceptConnectionsAsync(_cts.Token);
        }

        /// <summary>
        /// Stop the IPC server
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener = null;
        }

        private async Task AcceptConnectionsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener != null)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client, ct);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"VS²Sync: Error accepting connection: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    while (!ct.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null) break;

                        try
                        {
                            var message = JsonConvert.DeserializeObject<IpcMessage>(line);
                            if (message != null)
                            {
                                var response = await HandleMessageAsync(message);
                                if (response != null)
                                {
                                    await writer.WriteLineAsync(JsonConvert.SerializeObject(response));
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            Debug.WriteLine($"VS²Sync: JSON parse error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VS²Sync: Client handling error: {ex.Message}");
            }
        }

        private async Task<IpcMessage?> HandleMessageAsync(IpcMessage message)
        {
            switch (message.Type)
            {
                case "DISCOVER":
                    return await HandleDiscoverAsync(message);
                case "OPEN_FILE":
                    return await HandleOpenFileAsync(message);
                case "PING":
                    return IpcMessage.Create(MessageType.PONG, new { }, IdeType.visualstudio);
                default:
                    return null;
            }
        }

        private async Task<IpcMessage> HandleDiscoverAsync(IpcMessage message)
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
            var solutionPath = dte?.Solution?.FullName ?? string.Empty;
            var workspacePath = !string.IsNullOrEmpty(solutionPath)
                ? Path.GetDirectoryName(solutionPath) ?? string.Empty
                : GetOpenFolderPath(dte);

            var windowHandle = dte != null ? new IntPtr(dte.MainWindow.HWnd) : IntPtr.Zero;

            var response = new DiscoverResponsePayload
            {
                Port = _port,
                Ide = "visualstudio",
                Version = dte?.Version ?? "unknown",
                WorkspacePath = workspacePath,
                SolutionPath = solutionPath,
                Pid = System.Diagnostics.Process.GetCurrentProcess().Id,
                WindowHandle = windowHandle.ToInt64()
            };

            return IpcMessage.Create(MessageType.DISCOVER_RESPONSE, response, IdeType.visualstudio);
        }

        private string GetOpenFolderPath(DTE2? dte)
        {
            if (dte?.Solution == null) return string.Empty;

            // Try to get folder from solution path
            var solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
            if (!string.IsNullOrEmpty(solutionDir))
                return solutionDir;

            // For folder-based projects, try to get the first project's directory
            try
            {
                if (dte.Solution.Projects.Count > 0)
                {
                    var project = dte.Solution.Projects.Item(1);
                    if (project?.FullName != null)
                        return Path.GetDirectoryName(project.FullName) ?? string.Empty;
                }
            }
            catch { }

            return string.Empty;
        }

        private async Task<IpcMessage> HandleOpenFileAsync(IpcMessage message)
        {
            try
            {
                var payload = ((JObject)message.Payload).ToObject<OpenFilePayload>();
                if (payload == null)
                {
                    return CreateErrorResponse("Invalid payload");
                }

                await _package.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
                if (dte == null)
                {
                    return CreateErrorResponse("DTE service not available");
                }

                // Open the file
                var window = dte.ItemOperations.OpenFile(payload.FilePath);
                
                // Navigate to line/column if specified
                if (payload.Line.HasValue && payload.Line.Value > 0)
                {
                    var textDocument = dte.ActiveDocument?.Object("TextDocument") as TextDocument;
                    if (textDocument != null)
                    {
                        var editPoint = textDocument.StartPoint.CreateEditPoint();
                        var line = payload.Line.Value;
                        var column = payload.Column ?? 1;

                        try
                        {
                            var selection = dte.ActiveDocument.Selection as TextSelection;
                            selection?.MoveToLineAndOffset(line, column);
                        }
                        catch { }
                    }
                }

                // Focus the window if requested
                if (payload.Focus)
                {
                    await FocusWindowAsync(dte);
                }

                return IpcMessage.Create(MessageType.OPEN_FILE_RESPONSE, new OpenFileResponsePayload
                {
                    Success = true
                }, IdeType.visualstudio);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex.Message);
            }
        }

        private async Task FocusWindowAsync(DTE2 dte)
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var hwnd = new IntPtr(dte.MainWindow.HWnd);
                WindowHelper.FocusWindow(hwnd);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VS²Sync: Failed to focus window: {ex.Message}");
            }
        }

        private static IpcMessage CreateErrorResponse(string error)
        {
            return IpcMessage.Create(MessageType.OPEN_FILE_RESPONSE, new OpenFileResponsePayload
            {
                Success = false,
                Error = error
            }, IdeType.visualstudio);
        }
    }
}
