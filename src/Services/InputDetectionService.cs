// - Đăng ký sự kiện focus thông qua Win32 API
// - Phát hiện khi người dùng focus vào text field
// - Xác định loại control và vị trí của nó trên màn hình
// - Thông báo cho OverlayService hiển thị UI
using SpeechToTextAssistant.Helpers;
using System;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;

namespace SpeechToTextAssistant.Services
{
    public class InputDetectionService : IDisposable
    {
        #region Events
        public event EventHandler<InputFieldDetectedEventArgs> InputFieldDetected;
        #endregion

        #region Fields
        private IntPtr _hook;
        private Win32Interop.WinEventDelegate _winEventDelegate;
        private bool _isRunning;
        private HwndSource _hwndSource;
        #endregion

        public InputDetectionService()
        {
            // Initialize the input detection
            Start();
        }

        #region Public Methods
        public void Start()
        {
            if (_isRunning)
                return;

            _winEventDelegate = WinEventProc;

            // Hook both focus and value changes
            _hook = Win32Interop.SetWinEventHook(
                Win32Interop.EVENT_OBJECT_FOCUS,
                Win32Interop.EVENT_OBJECT_VALUECHANGE,
                IntPtr.Zero,
                _winEventDelegate,
                0, 0,
                Win32Interop.WINEVENT_OUTOFCONTEXT |
                Win32Interop.WINEVENT_SKIPOWNPROCESS);

            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            if (_hook != IntPtr.Zero)
            {
                Win32Interop.UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }

            _isRunning = false;
        }

        public void Dispose()
        {
            Stop();
        }
        #endregion

        #region Private Methods
        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            // Ignore non-client objects (we only want edit controls)
            if (idObject != Win32Interop.OBJID_CLIENT && idObject != 0)
                return;

            // Try to determine if this is a text input control
            if (IsTextInputControl(hwnd))
            {
                // Get the position of the input field
                Win32Interop.RECT rect;
                Win32Interop.GetWindowRect(hwnd, out rect);

                // Create the event args
                var args = new InputFieldDetectedEventArgs
                {
                    WindowHandle = hwnd,
                    Position = new Rect(rect.Left, rect.Top, rect.Width, rect.Height)
                };

                // Use UI Automation to get more info if needed
                try
                {
                    AutomationElement element = AutomationElement.FromHandle(hwnd);
                    if (element != null)
                    {
                        args.AutomationElement = element;

                        // Try to get control type
                        object controlType = element.GetCurrentPropertyValue(AutomationElement.ControlTypeProperty);
                        args.IsTextEditControl = IsEditableTextControl(element);
                    }
                }
                catch (Exception)
                {
                    // Error accessing UI Automation - just continue with what we have
                }

                // Only fire the event if we're reasonably sure this is a text input
                if (args.IsTextEditControl)
                {
                    InputFieldDetected?.Invoke(this, args);
                }
            }
        }

        private bool IsTextInputControl(IntPtr hwnd)
        {
            // Get class name, which can help identify standard controls
            StringBuilder className = new StringBuilder(256);
            Win32Interop.GetClassName(hwnd, className, className.Capacity);
            string classNameStr = className.ToString().ToLower();

            // Common Windows text control class names
            if (classNameStr.Contains("edit") ||
                classNameStr.Contains("text") ||
                classNameStr.Contains("richedit") ||
                classNameStr.Contains("treeview") ||
                classNameStr.Contains("syslistview32"))
            {
                return true;
            }

            // Try to use UI Automation for more complex cases
            try
            {
                AutomationElement element = AutomationElement.FromHandle(hwnd);
                return IsEditableTextControl(element);
            }
            catch
            {
                return false;
            }
        }

        private bool IsEditableTextControl(AutomationElement element)
        {
            if (element == null)
                return false;

            try
            {
                // Check for control patterns that suggest text editing capability
                object valuePattern = null;
                object textPattern = null;

                element.TryGetCurrentPattern(ValuePattern.Pattern, out valuePattern);
                element.TryGetCurrentPattern(TextPattern.Pattern, out textPattern);

                if (valuePattern != null || textPattern != null)
                {
                    var controlType = element.GetCurrentPropertyValue(AutomationElement.ControlTypeProperty);

                    // Check if control type is a known editable type
                    if (controlType.Equals(ControlType.Edit) ||
                        controlType.Equals(ControlType.Document))
                    {
                        return true;
                    }

                    // Check if it's a custom control that accepts text input
                    bool isKeyboardFocusable = (bool)element.GetCurrentPropertyValue(AutomationElement.IsKeyboardFocusableProperty);
                    bool isEnabled = (bool)element.GetCurrentPropertyValue(AutomationElement.IsEnabledProperty);

                    return isKeyboardFocusable && isEnabled;
                }
            }
            catch
            {
                // Error accessing UI Automation properties
            }

            return false;
        }
        #endregion
    }

    public class InputFieldDetectedEventArgs : EventArgs
    {
        public IntPtr WindowHandle { get; set; }
        public Rect Position { get; set; }
        public AutomationElement AutomationElement { get; set; }
        public bool IsTextEditControl { get; set; }
    }
}