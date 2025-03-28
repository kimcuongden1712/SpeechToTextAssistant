using Hardcodet.Wpf.TaskbarNotification;
using SpeechToTextAssistant.Infrastructures;
using SpeechToTextAssistant.Models;
using SpeechToTextAssistant.Services;
using SpeechToTextAssistant.src;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SpeechToTextAssistant
{
    public partial class MainWindow : Window
    {
        private InputDetectionService _inputDetectionService;

        private OverlayService _overlayService;

        private SpeechRecognitionService _speechRecognitionService;
        public ObservableCollection<string> DetectionLog { get; } = new ObservableCollection<string>();
        private TaskbarIcon _taskbarIcon;
        public MainWindow()
        {
            InitializeComponent();
            LogListView.ItemsSource = DetectionLog;

            _inputDetectionService = new InputDetectionService();
            _inputDetectionService.InputFieldDetected += OnInputFieldDetected;

            _overlayService = new OverlayService();
            _overlayService.MicrophoneButtonClicked += OnMicrophoneButtonClicked;
            _overlayService.OptionsButtonClicked += OnOptionsButtonClicked;
            _overlayService.SpeechRecognized += OnSpeechRecognized;
            _overlayService.RecognitionError += OnRecognitionError;

            _speechRecognitionService = new SpeechRecognitionService();
            _speechRecognitionService.RecognitionStarted += OnRecognitionStarted;
            _speechRecognitionService.RecognitionStopped += OnRecognitionStopped;
            _speechRecognitionService.TextRecognized += OnTextRecognized;
            _speechRecognitionService.RecognitionError += OnSpeechRecognitionError;
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
        /// <summary>
        /// This would eventually connect to SpeechRecognitionService
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMicrophoneButtonClicked(object sender, EventArgs e)
        {
            LogEvent("Microphone button clicked - Starting speech recognition");
            if (_speechRecognitionService.IsRecognizing)
            {
                _speechRecognitionService.StopRecognition();
                LogEvent("Stopping active recognition session");
            }
            else
            {
                _overlayService.ShowRecordingInProgress();
                _speechRecognitionService.StartRecognition();
            }
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
            InitializeSystemTray();
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            _inputDetectionService.Dispose();
            _overlayService.HideOverlay();
            _overlayService.Cleanup();
            if (_taskbarIcon != null)
            {
                _taskbarIcon.Dispose();
            }
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

        private void OnRecognitionStarted(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LogEvent("Speech recognition started");
            });
        }

        private void OnRecognitionStopped(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LogEvent("Speech recognition stopped");
                _overlayService.ShowRecordingStopped();
            });
        }

        private void OnTextRecognized(object sender, string recognizedText)
        {
            Dispatcher.Invoke(() =>
            {
                LogEvent($"Text recognized: \"{recognizedText}\"");
                // Có thể bạn sẽ muốn xử lý văn bản ở đây hoặc chuyển tiếp nó
            });
        }

        private void OnSpeechRecognitionError(object sender, string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                LogEvent($"Recognition service error: {errorMessage}");
            });
        }

        private void InitializeSystemTray()
        {
            _taskbarIcon = new TaskbarIcon
            {
                //IconSource = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/microphone.png")), TODO: Add icon
                ToolTipText = "Speech To Text Assistant"
            };

            // Tạo context menu kiểu WPF
            var contextMenu = new ContextMenu();

            // Tạo menu item "Bật/Tắt"
            var enableItem = new MenuItem { Header = "Bật/Tắt" };
            enableItem.Click += (s, e) =>
            {
                if (_inputDetectionService != null)
                {
                    if (PauseResumeButton.Content.ToString() == "Pause")
                    {
                        _inputDetectionService.Stop();
                        PauseResumeButton.Content = "Resume";
                        enableItem.Header = "Bật";
                    }
                    else
                    {
                        _inputDetectionService.Start();
                        PauseResumeButton.Content = "Pause";
                        enableItem.Header = "Tắt";
                    }
                }
            };

            // Tạo menu item "Cài đặt"
            var settingsItem = new MenuItem { Header = "Cài đặt" };
            settingsItem.Click += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };

            // Tạo menu item "Thoát"
            var exitItem = new MenuItem { Header = "Thoát" };
            exitItem.Click += (s, e) =>
            {
                Application.Current.Shutdown();
            };

            // Thêm các item vào context menu
            contextMenu.Items.Add(enableItem);
            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitItem);

            // Gán context menu cho taskbar icon
            _taskbarIcon.ContextMenu = contextMenu;

            // Xử lý sự kiện double-click
            _taskbarIcon.DoubleClickCommand = new RelayCommand(() =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            });
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }

            base.OnStateChanged(e);
        }
    }
}