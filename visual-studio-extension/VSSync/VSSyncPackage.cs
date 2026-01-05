using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VSSync
{
    /// <summary>
    /// VSSync Visual Studio Package
    /// Provides IPC communication with VS Code for file synchronization
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VSSyncPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.FolderOpened_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VSSyncPackage : AsyncPackage
    {
        /// <summary>
        /// VSSync Package GUID string
        /// </summary>
        public const string PackageGuidString = "d7b9c0e0-8c5d-4e4f-b8a7-0c1d2e3f4a5b";

        private IpcServer? _ipcServer;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Initialize and start IPC server
            _ipcServer = new IpcServer(this);
            await _ipcServer.StartAsync();

            // Register commands
            await OpenInVSCodeCommand.InitializeAsync(this);
        }

        /// <summary>
        /// Get the IPC server instance
        /// </summary>
        public IpcServer? GetIpcServer() => _ipcServer;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ipcServer?.Stop();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
