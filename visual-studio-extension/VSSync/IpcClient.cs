using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VSSync
{
    /// <summary>
    /// IPC Client for communicating with VS Code instances
    /// </summary>
    public class IpcClient
    {
        private const int BasePort = 52342;
        private const int PortRange = 100;
        private const int ConnectionTimeoutMs = 1000;
        private const int OperationTimeoutMs = 5000;

        /// <summary>
        /// Discover VS Code instances with matching workspace
        /// </summary>
        public async Task<List<IdeInstance>> DiscoverInstancesAsync(string workspacePath)
        {
            var instances = new List<IdeInstance>();
            var normalizedPath = NormalizePath(workspacePath);
            var tasks = new List<Task<IdeInstance?>>();

            for (int port = BasePort; port < BasePort + PortRange; port++)
            {
                tasks.Add(ProbePortAsync(port, normalizedPath));
            }

            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
            {
                if (result != null)
                {
                    instances.Add(result);
                }
            }

            return instances;
        }

        /// <summary>
        /// Discover all VS Code instances regardless of workspace
        /// </summary>
        public async Task<List<IdeInstance>> DiscoverAllInstancesAsync()
        {
            var instances = new List<IdeInstance>();
            var tasks = new List<Task<IdeInstance?>>();

            for (int port = BasePort; port < BasePort + PortRange; port++)
            {
                tasks.Add(ProbePortAllAsync(port));
            }

            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
            {
                if (result != null)
                {
                    instances.Add(result);
                }
            }

            return instances;
        }

        /// <summary>
        /// Send open file request to a VS Code instance
        /// </summary>
        public async Task<bool> OpenFileAsync(IdeInstance instance, string filePath, int? line = null, int? column = null)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync("127.0.0.1", instance.Port);
                    if (await Task.WhenAny(connectTask, Task.Delay(OperationTimeoutMs)) != connectTask)
                    {
                        return false;
                    }
                    await connectTask;

                    using (var stream = client.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        var message = IpcMessage.Create(MessageType.OPEN_FILE, new OpenFilePayload
                        {
                            FilePath = NormalizePath(filePath),
                            Line = line,
                            Column = column,
                            Focus = true
                        }, IdeType.visualstudio);

                        await writer.WriteLineAsync(JsonConvert.SerializeObject(message));

                        var readTask = reader.ReadLineAsync();
                        if (await Task.WhenAny(readTask, Task.Delay(OperationTimeoutMs)) != readTask)
                        {
                            return false;
                        }

                        var response = await readTask;
                        if (response != null)
                        {
                            var responseMsg = JsonConvert.DeserializeObject<IpcMessage>(response);
                            if (responseMsg?.Type == "OPEN_FILE_RESPONSE")
                            {
                                var payload = ((JObject)responseMsg.Payload).ToObject<OpenFileResponsePayload>();
                                return payload?.Success ?? false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VSSync: Error opening file in VS Code: {ex.Message}");
            }

            return false;
        }

        private async Task<IdeInstance?> ProbePortAsync(int port, string workspacePath)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync("127.0.0.1", port);
                    if (await Task.WhenAny(connectTask, Task.Delay(ConnectionTimeoutMs)) != connectTask)
                    {
                        return null;
                    }

                    try
                    {
                        await connectTask;
                    }
                    catch
                    {
                        return null;
                    }

                    using (var stream = client.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        var message = IpcMessage.Create(MessageType.DISCOVER, new DiscoverPayload
                        {
                            WorkspacePath = workspacePath
                        }, IdeType.visualstudio);

                        await writer.WriteLineAsync(JsonConvert.SerializeObject(message));

                        var readTask = reader.ReadLineAsync();
                        if (await Task.WhenAny(readTask, Task.Delay(ConnectionTimeoutMs)) != readTask)
                        {
                            return null;
                        }

                        var response = await readTask;
                        if (response != null)
                        {
                            var responseMsg = JsonConvert.DeserializeObject<IpcMessage>(response);
                            if (responseMsg?.Type == "DISCOVER_RESPONSE")
                            {
                                var payload = ((JObject)responseMsg.Payload).ToObject<DiscoverResponsePayload>();
                                if (payload != null && payload.Ide == "vscode" && PathsMatch(payload.WorkspacePath, workspacePath))
                                {
                                    return new IdeInstance
                                    {
                                        Port = payload.Port,
                                        Ide = IdeType.vscode,
                                        Version = payload.Version,
                                        WorkspacePath = payload.WorkspacePath,
                                        SolutionPath = payload.SolutionPath,
                                        Pid = payload.Pid,
                                        WindowHandle = new IntPtr(payload.WindowHandle)
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VSSync: Error probing port {port}: {ex.Message}");
            }

            return null;
        }

        private async Task<IdeInstance?> ProbePortAllAsync(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync("127.0.0.1", port);
                    if (await Task.WhenAny(connectTask, Task.Delay(ConnectionTimeoutMs)) != connectTask)
                    {
                        return null;
                    }

                    try
                    {
                        await connectTask;
                    }
                    catch
                    {
                        return null;
                    }

                    using (var stream = client.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        var message = IpcMessage.Create(MessageType.DISCOVER, new DiscoverPayload
                        {
                            WorkspacePath = string.Empty
                        }, IdeType.visualstudio);

                        await writer.WriteLineAsync(JsonConvert.SerializeObject(message));

                        var readTask = reader.ReadLineAsync();
                        if (await Task.WhenAny(readTask, Task.Delay(ConnectionTimeoutMs)) != readTask)
                        {
                            return null;
                        }

                        var response = await readTask;
                        if (response != null)
                        {
                            var responseMsg = JsonConvert.DeserializeObject<IpcMessage>(response);
                            if (responseMsg?.Type == "DISCOVER_RESPONSE")
                            {
                                var payload = ((JObject)responseMsg.Payload).ToObject<DiscoverResponsePayload>();
                                // Return any VS Code instance, regardless of workspace
                                if (payload != null && payload.Ide == "vscode")
                                {
                                    return new IdeInstance
                                    {
                                        Port = payload.Port,
                                        Ide = IdeType.vscode,
                                        Version = payload.Version,
                                        WorkspacePath = payload.WorkspacePath,
                                        SolutionPath = payload.SolutionPath,
                                        Pid = payload.Pid,
                                        WindowHandle = new IntPtr(payload.WindowHandle)
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VSSync: Error probing port {port}: {ex.Message}");
            }

            return null;
        }

        private static string NormalizePath(string path)
        {
            // Handle empty paths
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            // Normalize path separators and resolve .. and .
            var normalized = Path.GetFullPath(path);

            // Convert backslashes to forward slashes for consistency
            normalized = normalized.Replace("\\", "/");

            // Remove trailing separator (unless it's a root path like "/" or "C:/")
            if (normalized.Length > 1 && normalized.EndsWith("/"))
            {
                var withoutTrailing = normalized.Substring(0, normalized.Length - 1);
                // Keep trailing slash only for root paths like "C:"
                if (withoutTrailing.Length != 2 || withoutTrailing[1] != ':')
                {
                    normalized = withoutTrailing;
                }
            }

            return normalized.ToLowerInvariant();
        }

        private static bool PathsMatch(string path1, string path2)
        {
            var normalized1 = NormalizePath(path1);
            var normalized2 = NormalizePath(path2);

            return normalized1.StartsWith(normalized2) ||
                   normalized2.StartsWith(normalized1) ||
                   normalized1 == normalized2;
        }
    }
}
