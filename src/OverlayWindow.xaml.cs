using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SpeechToTextAssistant
{
    public partial class OverlayWindow : Window
    {
        // Events
        public event EventHandler MicrophoneButtonClicked;
        public event EventHandler OptionsButtonClicked;
        public event EventHandler UserInteracted;

        // Current state
        private bool _isRecording = false;
        // For status messages
        private DispatcherTimer _statusTimer;
        public OverlayWindow()
        {
            InitializeComponent();
            // Set up status message timer
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(3);
            _statusTimer.Tick += (s, e) =>
            {
                _statusTimer.Stop();
                HideStatusMessage();
            };
            // Make window click-through when not directly interacting with it
            this.MouseEnter += (s, e) =>
            {
                this.Opacity = 0.95;
                UserInteracted?.Invoke(this, EventArgs.Empty);
            };
            this.MouseLeave += (s, e) => { this.Opacity = 0.8; };

            // Allow dragging
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    this.DragMove();
            };
            // Make window click-through when not directly interacting with it
            this.MouseEnter += (s, e) =>
            {
                this.Opacity = 0.95;
                UserInteracted?.Invoke(this, EventArgs.Empty);
            };


            // Allow dragging
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    this.DragMove();
                    UserInteracted?.Invoke(this, EventArgs.Empty);
                }
            };

            // Reset timer on any button click
            this.MicButton.Click += (s, e) => UserInteracted?.Invoke(this, EventArgs.Empty);
            this.OptionsButton.Click += (s, e) => UserInteracted?.Invoke(this, EventArgs.Empty);
        }

        private void MicButton_Click(object sender, RoutedEventArgs e)
        {
            _isRecording = !_isRecording;
            UpdateMicButtonState();
            MicrophoneButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            OptionsButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateMicButtonState()
        {
            // Update the mic button appearance based on recording state
            if (_isRecording)
            {
                // Change to recording state (red)
                var circle = FindChild<System.Windows.Shapes.Ellipse>(MicButton, "Circle");
                if (circle != null)
                {
                    circle.Fill = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                    circle.Stroke = new SolidColorBrush(Color.FromRgb(220, 50, 50));
                }

                var icon = FindChild<System.Windows.Shapes.Path>(MicButton, "MicIcon");
                if (icon != null)
                {
                    icon.Fill = new SolidColorBrush(Color.FromRgb(220, 50, 50));
                }

                MicButton.ToolTip = "Click to stop recording (Ctrl+M)";
            }
            else
            {
                // Reset to default state
                var circle = FindChild<System.Windows.Shapes.Ellipse>(MicButton, "Circle");
                if (circle != null)
                {
                    circle.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    circle.Stroke = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                }

                var icon = FindChild<System.Windows.Shapes.Path>(MicButton, "MicIcon");
                if (icon != null)
                {
                    icon.Fill = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                }

                MicButton.ToolTip = "Click to start recording (Ctrl+M)";
            }
        }

        public void ShowRecordingInProgress()
        {
            _isRecording = true;
            UpdateMicButtonState();
            ShowStatusMessage("Listening...", Colors.DarkGreen);
        }

        public void ShowRecordingStopped()
        {
            _isRecording = false;
            UpdateMicButtonState();
            ShowStatusMessage("Stopped listening", Colors.DarkGray);
        }

        // Helper method to find child controls by name
        private static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // If the child is not of the request child type, recurse
                T childType = child as T;
                if (childType == null)
                {
                    // Search in the child's children
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    // If the child's name is set for search
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // If the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // No name is specified
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public void ShowRecognitionSuccess(string text)
        {
            // Show a brief success message with the recognized text
            if (text.Length > 20)
            {
                text = text.Substring(0, 20) + "...";
            }

            ShowStatusMessage($"Recognized: \"{text}\"", Colors.DarkGreen);
        }

        public void ShowRecognitionError(string error)
        {
            // Show error message
            ShowStatusMessage("Error: " + error, Colors.DarkRed);
        }

        private void ShowStatusMessage(string message, Color color)
        {
            // Show the status panel with message
            StatusText.Text = message;
            StatusText.Foreground = new SolidColorBrush(color);

            // Make sure panel is visible
            if (StatusPanel.Visibility != Visibility.Visible)
            {
                StatusPanel.Visibility = Visibility.Visible;
                StatusPanel.Opacity = 0;

                // Animate in
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                StatusPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }

            // Reset the timer
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void HideStatusMessage()
        {
            // Animate out
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => StatusPanel.Visibility = Visibility.Collapsed;
            StatusPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

    }
}