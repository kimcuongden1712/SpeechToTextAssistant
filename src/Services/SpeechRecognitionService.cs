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
                    new System.Globalization.CultureInfo("en-US"));

                // Load dictation grammar for free-text speech recognition
                _recognitionEngine.LoadGrammar(new DictationGrammar());

                // Set up event handlers
                _recognitionEngine.SpeechRecognized += OnSpeechRecognized;
                _recognitionEngine.SpeechRecognitionRejected += OnSpeechRecognitionRejected;

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
            if (_isRecognizing) return false;

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
            if (!_isRecognizing) return false;

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
                    SimulateTextInput(element, text);
                    return;
                }

                // Fall back to simulating keyboard input
                SimulateTextInput(element, text);
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
                SimulateTextInput(element, text);
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Failed to append text: {ex.Message}");
            }
        }

        private void SimulateTextInput(AutomationElement element, string text)
        {
            // Focus the element first
            element.SetFocus();

            // Use SendKeys to input text (requires System.Windows.Forms reference)
            //System.Windows.Forms.SendKeys.SendWait(text);
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence > 0.3) // Minimum confidence threshold
            {
                var result = new SpeechRecognitionResult(e.Result.Text, e.Result.Confidence);
                SpeechRecognized?.Invoke(this, result);
            }
        }

        private void OnSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            RecognitionError?.Invoke(this, "Speech recognition rejected or not understood");
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