using System;
using Newtonsoft.Json;

namespace VSSync
{
    /// <summary>
    /// IPC Message Types
    /// </summary>
    public enum MessageType
    {
        DISCOVER,
        DISCOVER_RESPONSE,
        OPEN_FILE,
        OPEN_FILE_RESPONSE,
        PING,
        PONG
    }

    /// <summary>
    /// IDE Type
    /// </summary>
    public enum IdeType
    {
        vscode,
        visualstudio
    }

    /// <summary>
    /// Base IPC Message structure
    /// </summary>
    public class IpcMessage<T>
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("payload")]
        public T Payload { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("sourceIde")]
        public string SourceIde { get; set; } = string.Empty;

        [JsonProperty("sourcePid")]
        public int SourcePid { get; set; }

        public static IpcMessage<Tmessage> Create<Tmessage>(MessageType type, Tmessage payload, IdeType ide)
        {
            return new IpcMessage<Tmessage>
            {
                Type = type.ToString(),
                Payload = payload,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SourceIde = ide.ToString(),
                SourcePid = System.Diagnostics.Process.GetCurrentProcess().Id
            };
        }
    }

    /// <summary>
    /// Discover request payload
    /// </summary>
    public class DiscoverPayload
    {
        [JsonProperty("workspacePath")]
        public string WorkspacePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Discover response payload
    /// </summary>
    public class DiscoverResponsePayload
    {
        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("ide")]
        public string Ide { get; set; } = "visualstudio";

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("workspacePath")]
        public string WorkspacePath { get; set; } = string.Empty;

        [JsonProperty("solutionPath")]
        public string? SolutionPath { get; set; }

        [JsonProperty("pid")]
        public int Pid { get; set; }

        [JsonProperty("windowHandle")]
        public long WindowHandle { get; set; }
    }

    /// <summary>
    /// Open file request payload
    /// </summary>
    public class OpenFilePayload
    {
        [JsonProperty("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonProperty("line")]
        public int? Line { get; set; }

        [JsonProperty("column")]
        public int? Column { get; set; }

        [JsonProperty("focus")]
        public bool Focus { get; set; } = true;
    }

    /// <summary>
    /// Open file response payload
    /// </summary>
    public class OpenFileResponsePayload
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// IDE Instance information
    /// </summary>
    public class IdeInstance
    {
        public int Port { get; set; }
        public IdeType Ide { get; set; }
        public string Version { get; set; } = string.Empty;
        public string WorkspacePath { get; set; } = string.Empty;
        public string? SolutionPath { get; set; }
        public int Pid { get; set; }
        public IntPtr WindowHandle { get; set; }
    }
}
