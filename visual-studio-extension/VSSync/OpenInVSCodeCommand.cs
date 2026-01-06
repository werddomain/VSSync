using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;


namespace VSSync
{
    /// <summary>
    /// Command handler for "Open in VS Code"
    /// </summary>
    internal sealed class OpenInVSCodeCommand
    {
        /// <summary>
        /// Command ID
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID)
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d7b9c0e0-8c5d-4e4f-b8a7-0c1d2e3f4a5c");

        /// <summary>
        /// VS Package that provides this command
        /// </summary>
        private readonly VSSyncPackage _package;

        /// <summary>
        /// IPC Client for communicating with VS Code
        /// </summary>
        private readonly IpcClient _ipcClient;

        /// <summary>
        /// Cache for instance selection (session storage)
        /// </summary>
        private readonly Dictionary<string, IdeInstance> _instanceChoiceCache = new Dictionary<string, IdeInstance>();

        /// <summary>
        /// Gets the instance of the command
        /// </summary>
        public static OpenInVSCodeCommand? Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package
        /// </summary>
        private IAsyncServiceProvider ServiceProvider => _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenInVSCodeCommand"/> class
        /// </summary>
        private OpenInVSCodeCommand(VSSyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _ipcClient = new IpcClient();

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandID);
            menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Initializes the singleton instance of the command
        /// </summary>
        public static async Task InitializeAsync(VSSyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                Instance = new OpenInVSCodeCommand(package, commandService);
            }
        }

        /// <summary>
        /// Update command visibility/enabled state
        /// </summary>
        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                // Enable when there's an active document
                var dte = (DTE2)Package.GetGlobalService(typeof(DTE));
                command.Enabled = dte?.ActiveDocument != null;
                command.Visible = true;
            }
        }

        /// <summary>
        /// Execute the command
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _ = ExecuteAsync();
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = await ServiceProvider.GetServiceAsync(typeof(DTE)) as DTE2;
                if (dte?.ActiveDocument == null)
                {
                    VsShellUtilities.ShowMessageBox(
                        _package,
                        "No active document to open.",
                        "VS²Sync",
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                var filePath = dte.ActiveDocument.FullName;
                var selection = dte.ActiveDocument.Selection as TextSelection;
                int? line = selection?.CurrentLine;
                int? column = selection?.CurrentColumn;

                var solutionPath = dte.Solution?.FullName ?? string.Empty;
                var workspacePath = !string.IsNullOrEmpty(solutionPath)
                    ? Path.GetDirectoryName(solutionPath) ?? string.Empty
                    : string.Empty;

                if (string.IsNullOrEmpty(workspacePath))
                {
                    VsShellUtilities.ShowMessageBox(
                        _package,
                        "No solution or folder is open.",
                        "VS²Sync",
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                // Show status
                dte.StatusBar.Text = "VS²Sync: Searching for VS Code instances...";
                dte.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationFind);

                try
                {
                    // Discover VS Code instances
                    var instances = await _ipcClient.DiscoverInstancesAsync(workspacePath);

                    if (instances.Count == 0)
                    {
                        dte.StatusBar.Text = "VS²Sync: No VS Code instance found";
                        dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationFind);

                        VsShellUtilities.ShowMessageBox(
                            _package,
                            "No VS Code instance found with the same workspace open.\n\n" +
                            "Make sure VS Code has the VS²Sync extension installed and the same folder open.",
                            "VS²Sync",
                            OLEMSGICON.OLEMSGICON_WARNING,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        return;
                    }

                    // Select instance
                    IdeInstance? selectedInstance;

                    if (instances.Count == 1)
                    {
                        selectedInstance = instances[0];
                    }
                    else
                    {
                        // Check cache first
                        if (_instanceChoiceCache.TryGetValue(workspacePath, out var cachedInstance))
                        {
                            if (instances.Any(i => i.Pid == cachedInstance.Pid))
                            {
                                selectedInstance = cachedInstance;
                            }
                            else
                            {
                                selectedInstance = await PromptInstanceSelectionAsync(instances);
                            }
                        }
                        else
                        {
                            selectedInstance = await PromptInstanceSelectionAsync(instances);
                        }

                        if (selectedInstance != null)
                        {
                            _instanceChoiceCache[workspacePath] = selectedInstance;
                        }
                    }

                    if (selectedInstance == null)
                    {
                        dte.StatusBar.Text = "VS²Sync: Operation cancelled";
                        dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationFind);
                        return;
                    }

                    dte.StatusBar.Text = "VS²Sync: Opening file in VS Code...";

                    // Open the file
                    var success = await _ipcClient.OpenFileAsync(selectedInstance, filePath, line, column);

                    dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationFind);

                    if (success)
                    {
                        dte.StatusBar.Text = "VS²Sync: File opened in VS Code";
                    }
                    else
                    {
                        dte.StatusBar.Text = "VS²Sync: Failed to open file";
                        VsShellUtilities.ShowMessageBox(
                            _package,
                            "Failed to open file in VS Code.",
                            "VS²Sync",
                            OLEMSGICON.OLEMSGICON_WARNING,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                }
                finally
                {
                    dte.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationFind);
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"An error occurred: {ex.Message}",
                    "VS²Sync Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private async Task<IdeInstance?> PromptInstanceSelectionAsync(List<IdeInstance> instances)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Build description list for the dialog
            var descriptions = instances.Select((inst, idx) =>
                $"{idx + 1}. VS Code {inst.Version}\n   Path: {inst.WorkspacePath}\n   PID: {inst.Pid}").ToList();

            var message = $"Multiple VS Code instances found ({instances.Count}).\n\n" +
                          string.Join("\n\n", descriptions) + "\n\n" +
                          "The first matching instance will be used.\n" +
                          "To use a different instance, close other VS Code windows and try again.";

            // Show informational dialog. For a more sophisticated implementation,
            // a custom WPF dialog could be used to allow explicit selection.
            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                "VS²Sync - Multiple Instances Found",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            // Return the first instance (most recently discovered, likely most recently active)
            return instances.FirstOrDefault();
        }
    }
}
