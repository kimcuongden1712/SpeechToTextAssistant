using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace SpeechToTextAssistant.Helpers
{
    public static class InputSimulatorHelper
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const byte VK_CONTROL = 0x11; // Ctrl key
        private const byte VK_V = 0x56;       // V key
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        /// <summary>
        /// Simulates pressing Ctrl+V to paste from clipboard
        /// </summary>
        public static void PasteFromClipboard()
        {
            // Press Ctrl key down
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

            // Press V key down
            keybd_event(VK_V, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

            // Small delay
            Thread.Sleep(50);

            // Release V key
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Release Ctrl key
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Allow time for paste to complete
            Thread.Sleep(50);
        }
    }
}