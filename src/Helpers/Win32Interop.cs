// Sử dụng P/Invoke để gọi các API Windows:
// - SetWinEventHook để theo dõi sự kiện focus
// - GetWindowRect để xác định vị trí cửa sổ
// - GetGUIThreadInfo để lấy thông tin về control đang được focus
using System;
using System.Runtime.InteropServices;

namespace SpeechToTextAssistant.Helpers
{
    public static class Win32Interop
    {
        #region Constants
        // Window message constants
        public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        public const uint EVENT_OBJECT_FOCUS = 0x0008;
        public const uint EVENT_OBJECT_SELECTION = 0x8006;
        public const uint EVENT_OBJECT_VALUECHANGE = 0x800E;

        public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        public const uint WINEVENT_SKIPOWNTHREAD = 0x0001;
        public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        // Control type identifiers
        public const uint OBJID_CLIENT = 0xFFFFFFFC;
        #endregion

        #region Structs
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public uint flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }
        #endregion

        #region Delegates
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime);
        #endregion

        #region Methods
        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        public static string GetActiveWindowTitle()
        {
            IntPtr handle = GetForegroundWindow();
            int length = GetWindowTextLength(handle);
            System.Text.StringBuilder builder = new System.Text.StringBuilder(length + 1);
            GetWindowText(handle, builder, builder.Capacity);
            return builder.ToString();
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);
        #endregion
    }
}