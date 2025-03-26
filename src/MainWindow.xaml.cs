using SpeechToTextAssistant.Models;
using SpeechToTextAssistant.Services;
using SpeechToTextAssistant.src;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace SpeechToTextAssistant
{
    public partial class MainWindow : Window
    {
        private InputDetectionService _inputDetectionService;
        public ObservableCollection<string> DetectionLog { get; } = new ObservableCollection<string>();
        // Update the MainWindow class to add OverlayService integration

        private OverlayService _overlayService;
        public MainWindow()
        {
            InitializeComponent();
            LogListView.ItemsSource = DetectionLog;

            // Initialize the detection service
            _inputDetectionService = new InputDetectionService();
            _inputDetectionService.InputFieldDetected += OnInputFieldDetected;
            // Initialize the services
            _overlayService = new OverlayService();
            _overlayService.MicrophoneButtonClicked += OnMicrophoneButtonClicked;
            _overlayService.OptionsButtonClicked += OnOptionsButtonClicked;
            _overlayService.SpeechRecognized += OnSpeechRecognized;
            _overlayService.RecognitionError += OnRecognitionError;
        }

        private void OnInputFieldDetected(object sender, InputFieldDetectedEventArgs e)
        {
            // Update the UI on the UI thread
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Input field detected at: {e.Position.X}, {e.Position.Y}";
                PositionInfoText.Text = $"Width: {e.Position.Width}, Height: {e.Position.Height}";

                string controlInfo = "Unknown control";
                if (e.AutomationElement != null)
                {
                    try
                    {
                        string name = e.AutomationElement.Current.Name;
                        string controlType = e.AutomationElement.Current.ControlType.ProgrammaticName;
                        controlInfo = $"Control: {name}, Type: {controlType}";
                        ControlInfoText.Text = controlInfo;
                    }
                    catch (Exception ex)
                    {
                        controlInfo = $"Error: {ex.Message}";
                        ControlInfoText.Text = "Error getting automation information";
                    }
                }

                LogEvent($"Detected: {controlInfo} at ({e.Position.X}, {e.Position.Y})");

                // Show the overlay next to the input field
                if (e.IsTextEditControl)
                {
                    _overlayService.ShowOverlay(e.Position, e.AutomationElement);
                }
            });
        }

        private void LogEvent(string message)
        {
            string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            DetectionLog.Add(logEntry);

            // Auto-scroll to bottom
            if (LogListView.Items.Count > 0)
            {
                LogListView.ScrollIntoView(LogListView.Items[LogListView.Items.Count - 1]);
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            DetectionLog.Clear();
        }

        private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_inputDetectionService != null)
            {
                if (PauseResumeButton.Content.ToString() == "Pause")
                {
                    _inputDetectionService.Stop();
                    PauseResumeButton.Content = "Resume";
                    StatusText.Text = "Monitoring paused";
                    LogEvent("Detection service paused");
                }
                else
                {
                    _inputDetectionService.Start();
                    PauseResumeButton.Content = "Pause";
                    StatusText.Text = "Monitoring for input fields...";
                    LogEvent("Detection service resumed");
                }
            }
        }
        private void OnMicrophoneButtonClicked(object sender, EventArgs e)
        {
            // This would eventually connect to SpeechRecognitionService
            LogEvent("Microphone button clicked");
        }

        private void OnOptionsButtonClicked(object sender, EventArgs e)
        {
            LogEvent("Options button clicked");
            // Example: Show a context menu with speech recognition options
            // Would be expanded in a future implementation
        }
        /// <summary>
        /// Start monitoring for text input fields
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _inputDetectionService.Start();
            StatusText.Text = "Monitoring for input fields...";
            LogEvent("Detection service started");
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            _inputDetectionService.Dispose();
            _overlayService.HideOverlay();
            _overlayService.Cleanup();
        }

        private void TestFieldButton_Click(object sender, RoutedEventArgs e)
        {
            FormTest a = new FormTest();
            a.Show();
            LogEvent("Opened test form");
        }
        // Add these methods to the MainWindow class


        private void OnSpeechRecognized(object sender, SpeechRecognitionResult e)
        {
            LogEvent($"Speech recognized: \"{e.RecognizedText}\" (Confidence: {e.Confidence:P})");
        }

        private void OnRecognitionError(object sender, string e)
        {
            LogEvent($"Recognition error: {e}");
        }
    }
}