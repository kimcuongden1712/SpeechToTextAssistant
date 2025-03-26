// - Sử dụng System.Windows.Automation để tìm và thao tác với UI elements
// - Xác định loại control (textbox, input field)
// - Đọc và ghi giá trị vào control
using System;
using System.Windows.Automation;

namespace SpeechToTextAssistant.Helpers
{
    public static class UIAutomationHelper
    {
        public static AutomationElement GetFocusedElement()
        {
            return AutomationElement.FocusedElement;
        }

        public static string GetElementName(AutomationElement element)
        {
            if (element == null)
                return string.Empty;

            var nameProperty = element.GetCurrentPropertyValue(AutomationElement.NameProperty);
            return nameProperty as string ?? string.Empty;
        }

        public static void SetFocusToElement(AutomationElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var invokePattern = element.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            invokePattern?.Invoke();
        }
    }
}