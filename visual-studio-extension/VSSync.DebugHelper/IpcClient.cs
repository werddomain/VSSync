using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VSSync.DebugHelper;

/// <summary>
/// IPC Client for discovering and communicating with IDE instances
/// </summary>
public class IpcClient
{
    private const int BasePort = 52342;
    private const int PortRange = 100;
    private const int ConnectionTimeoutMs = 1000;
    private const int OperationTimeoutMs = 5000;

    public event EventHandler<string>? LogMessage;

    private void Log(string message)
    {
        LogMessage?.Invoke(this, $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    /// <summary>
    /// Discover ALL IDE instances (both VS and VS Code)
    /// </summary>
    public async Task<List<IdeInstance>> DiscoverAllInstancesAsync()
    {
        var instances = new List<IdeInstance>();
        var tasks = new List<Task<IdeInstance?>>();

        Log($"Starting discovery on ports {BasePort} to {BasePort + PortRange - 1}...");

        for (int port = BasePort; port < BasePort + PortRange; port++)
        {
            tasks.Add(ProbePortAsync(port));
        }

        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            if (result != null)
            {
                instances.Add(result);
                Log($"Found instance: {result}");
            }
        }

        Log($"Discovery complete. Found {instances.Count} instance(s).");
        return instances;
    }

    /// <summary>
    /// Send open file request to an IDE instance
    /// </summary>
    public async Task<(bool Success, string? Error)> OpenFileAsync(IdeInstance instance, string filePath, int? line = null, int? column = null)
    {
        try
        {
            Log($"Connecting to {instance.Ide} on port {instance.Port}...");

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync("127.0.0.1", instance.Port);
            if (await Task.WhenAny(connectTask, Task.Delay(OperationTimeoutMs)) != connectTask)
            {
                return (false, "Connection timeout");
            }
            await connectTask;

            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            var normalizedPath = NormalizePath(filePath);
            var message = IpcMessage.Create(MessageType.OPEN_FILE, new OpenFilePayload
            {
                FilePath = normalizedPath,
                Line = line,
                Column = column,
                Focus = true
            }, "debughelper");

            var json = JsonConvert.SerializeObject(message);
            Log($"Sending OPEN_FILE: {json}");
            await writer.WriteLineAsync(json);

            var readTask = reader.ReadLineAsync();
            if (await Task.WhenAny(readTask, Task.Delay(OperationTimeoutMs)) != readTask)
            {
                return (false, "Read timeout");
            }

            var response = await readTask;
            Log($"Received response: {response}");

            if (response != null)
            {
                var responseMsg = JsonConvert.DeserializeObject<IpcMessage>(response);
                if (responseMsg?.Type == "OPEN_FILE_RESPONSE")
                {
                    var payload = ((JObject)responseMsg.Payload).ToObject<OpenFileResponsePayload>();
                    if (payload?.Success == true)
                    {
                        return (true, null);
                    }
                    return (false, payload?.Error ?? "Unknown error");
                }
            }

            return (false, "Invalid response");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
            return (false, ex.Message);
        }
    }

    private async Task<IdeInstance?> ProbePortAsync(int port)
    {
        try
        {
            using var client = new TcpClient();
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

            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            var message = IpcMessage.Create(MessageType.DISCOVER, new DiscoverPayload
            {
                WorkspacePath = string.Empty
            }, "debughelper");

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
                    if (payload != null)
                    {
                        Log($"Port {port}: {payload.Ide} (v{payload.Version}) PID:{payload.Pid} Workspace:{payload.WorkspacePath}");
                        return new IdeInstance
                        {
                            Port = payload.Port,
                            Ide = payload.Ide,
                            Version = payload.Version,
                            WorkspacePath = payload.WorkspacePath,
                            SolutionPath = payload.SolutionPath,
                            Pid = payload.Pid,
                            WindowHandle = payload.WindowHandle
                        };
                    }
                }
            }
        }
        catch
        {
            // Port not listening or error - ignore
        }

        return null;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var normalized = Path.GetFullPath(path);
        normalized = normalized.Replace("\\", "/");

        if (normalized.Length > 1 && normalized.EndsWith("/"))
        {
            var withoutTrailing = normalized[..^1];
            if (withoutTrailing.Length != 2 || withoutTrailing[1] != ':')
            {
                normalized = withoutTrailing;
            }
        }

        return normalized.ToLowerInvariant();
    }
}
