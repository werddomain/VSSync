using System;
using System.Runtime.InteropServices;

namespace VSSync
{
    /// <summary>
    /// Helper class for Windows window management using Win32 API
    /// </summary>
    public static class WindowHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        /// <summary>
        /// Force focus a window, using various Win32 techniques to ensure it comes to foreground
        /// </summary>
        public static bool FocusWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            try
            {
                // If window is minimized, restore it
                if (IsIconic(hWnd))
                {
                    ShowWindow(hWnd, SW_RESTORE);
                }

                // Get the current foreground window's thread
                IntPtr foregroundWindow = GetForegroundWindow();
                uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
                uint currentThreadId = GetCurrentThreadId();
                uint targetThreadId = GetWindowThreadProcessId(hWnd, out _);

                // Attach threads to allow setting foreground window
                bool threadAttached = false;
                if (foregroundThreadId != currentThreadId)
                {
                    threadAttached = AttachThreadInput(currentThreadId, foregroundThreadId, true);
                }

                try
                {
                    // Try multiple methods to bring window to front
                    
                    // Method 1: SetWindowPos with TOPMOST, then NOTOPMOST (flash technique)
                    SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

                    // Method 2: BringWindowToTop
                    BringWindowToTop(hWnd);

                    // Method 3: SetForegroundWindow
                    bool result = SetForegroundWindow(hWnd);

                    // Method 4: ShowWindow
                    ShowWindow(hWnd, SW_SHOW);

                    return result;
                }
                finally
                {
                    // Detach threads
                    if (threadAttached)
                    {
                        AttachThreadInput(currentThreadId, foregroundThreadId, false);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
