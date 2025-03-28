using SpeechToTextAssistant.Helpers;
using SpeechToTextAssistant.Models;
using System;
using System.Speech.Recognition;
using System.Windows.Automation;

namespace SpeechToTextAssistant.Services
{
    public class SpeechRecognitionService : IDisposable
    {
        private SpeechRecognitionEngine _recognitionEngine;
        private bool _isRecognizing = false;

        // Events
        public event EventHandler<SpeechRecognitionResult> SpeechRecognized;
        public event EventHandler RecognitionStarted;
        public event EventHandler RecognitionStopped;
        public event EventHandler<string> RecognitionError;

        public bool IsRecognizing => _isRecognizing;

        public Action<object, string> TextRecognized { get; set; }

        public SpeechRecognitionService()
        {
            InitializeSpeechRecognition();
        }

        /// <summary>
        /// Initialize the speech recognition engine
        /// </summary>
        private void InitializeSpeechRecognition()
        {
            try
            {
                _recognitionEngine = new SpeechRecognitionEngine(
                    new System.Globalization.CultureInfo("vi-VN"));

                // Load dictation grammar for free-text speech recognition
                _recognitionEngine.LoadGrammar(new DictationGrammar());

                // Đăng ký các sự kiện nhận dạng
                _recognitionEngine.SpeechRecognized += Recognizer_SpeechRecognized;
                _recognitionEngine.RecognizeCompleted += Recognizer_RecognizeCompleted;
                _recognitionEngine.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;

                // Set input to default audio device
                _recognitionEngine.SetInputToDefaultAudioDevice();
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Failed to initialize speech recognition: {ex.Message}");
            }
        }

        public bool StartRecognition()
        {
            if ((_recognitionEngine is null) || _isRecognizing) return false;

            try
            {
                _recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                _isRecognizing = true;
                RecognitionStarted?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Failed to start speech recognition: {ex.Message}");
                return false;
            }
        }

        public bool StopRecognition()
        {
            if ((_recognitionEngine is null) || !_isRecognizing) return false;

            try
            {
                _recognitionEngine.RecognizeAsyncStop();
                _isRecognizing = false;
                RecognitionStopped?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Failed to stop speech recognition: {ex.Message}");
                return false;
            }
        }

        public void InsertTextIntoFocusedElement(AutomationElement element, string text)
        {
            if (element == null || string.IsNullOrEmpty(text))
                return;

            try
            {
                // Try to use the ValuePattern first (most common for text fields)
                object valuePattern;
                if (element.TryGetCurrentPattern(ValuePattern.Pattern, out valuePattern))
                {
                    ((ValuePattern)valuePattern).SetValue(text);
                    return;
                }

                // Try to use TextPattern if ValuePattern is not available
                object textPattern;
                if (element.TryGetCurrentPattern(TextPattern.Pattern, out textPattern))
                {
                    // For TextPattern, we need more complex handling
                    // This is simplified - real implementation may need to handle selection, etc.
                    // For now, just simulate keyboard input
                    SendTextViaKeyboard(element, text);
                    return;
                }

                // Dùng mô phỏng phím tắt để paste text
                System.Windows.Clipboard.SetText(text);
                InputSimulatorHelper.PasteFromClipboard();// Ctrl+V
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Failed to insert text: {ex.Message}");
            }
        }

        public void AppendTextToFocusedElement(AutomationElement element, string text)
        {
            if (element == null || string.IsNullOrEmpty(text))
                return;

            try
            {
                // Try to use the ValuePattern first
                object valuePattern;
                if (element.TryGetCurrentPattern(ValuePattern.Pattern, out valuePattern))
                {
                    var existingText = ((ValuePattern)valuePattern).Current.Value;
                    ((ValuePattern)valuePattern).SetValue(existingText + text);
                    return;
                }

                // Fall back to simulating keyboard input to append
                SendTextViaKeyboard(element, text);
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Failed to append text: {ex.Message}");
            }
        }

        private void SendTextViaKeyboard(AutomationElement element, string text)
        {
            // Focus the element first
            element.SetFocus();

            // Lưu clipboard hiện tại
            string currentClipboard = null;
            if (System.Windows.Clipboard.ContainsText())
            {
                currentClipboard = System.Windows.Clipboard.GetText();
            }

            // Đặt văn bản nhận dạng vào clipboard
            System.Windows.Clipboard.SetText(text);

            // Sử dụng UI Automation để thực hiện thao tác paste
            try
            {
                // Sử dụng UI Automation để gửi Ctrl+V
                InputSimulatorHelper.PasteFromClipboard();
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Failed to simulate keyboard input: {ex.Message}");
            }

            // Khôi phục clipboard
            if (!string.IsNullOrEmpty(currentClipboard))
            {
                System.Windows.Clipboard.SetText(currentClipboard);
            }
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result != null && e.Result.Text.Length > 0 && e.Result.Confidence > 0.5)
            {
                TextRecognized?.Invoke(this, e.Result.Text);
            }
        }

        private void Recognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            RecognitionError?.Invoke(this, "Speech recognition rejected or not understood");
        }

        private void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                RecognitionError?.Invoke(this, $"Lỗi nhận dạng: {e.Error.Message}");
            }

            _isRecognizing = false;
            RecognitionStopped?.Invoke(this, EventArgs.Empty);
        }
        public void Dispose()
        {
            if (_recognitionEngine != null)
            {
                StopRecognition();
                _recognitionEngine.Dispose();
                _recognitionEngine = null;
            }
        }
    }
}