using SpeechToTextAssistant.Models;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Automation;

namespace SpeechToTextAssistant.Services
{

    public class OverlayService
    {
        private Timer _hideTimer;
        private const int HideDelayMs = 5000; // 5 seconds
        private OverlayWindow _overlayWindow;
        private bool _isVisible = false;
        private Point _lastPosition;
        private AutomationElement _currentElement;
        private SpeechRecognitionService _speechService;

        // Events that the main app can subscribe to
        public event EventHandler MicrophoneButtonClicked;
        public event EventHandler OptionsButtonClicked;
        public event EventHandler<SpeechRecognitionResult> SpeechRecognized;
        public event EventHandler<string> RecognitionError;

        public OverlayService()
        {
            InitializeOverlayWindow();
            InitializeSpeechService();
            _hideTimer = new System.Threading.Timer(HideTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

        }
        private void InitializeSpeechService()
        {
            _speechService = new SpeechRecognitionService();

            _speechService.SpeechRecognized += (s, result) =>
            {
                // Forward the event
                SpeechRecognized?.Invoke(this, result);

                // Insert the text into the current element
                if (_currentElement != null)
                {
                    _speechService.AppendTextToFocusedElement(_currentElement, result.RecognizedText);
                }

                // Update UI to show success
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _overlayWindow.ShowRecognitionSuccess(result.RecognizedText);
                });
            };

            _speechService.RecognitionStarted += (s, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _overlayWindow.ShowRecordingInProgress();
                });

                // Reset hide timer when recording starts
                ResetHideTimer();
            };

            _speechService.RecognitionStopped += (s, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _overlayWindow.ShowRecordingStopped();
                });
            };

            _speechService.RecognitionError += (s, errorMessage) =>
            {
                RecognitionError?.Invoke(this, errorMessage);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _overlayWindow.ShowRecognitionError(errorMessage);
                });
            };
        }
        /// <summary>
        /// Initialize the overlay window
        /// </summary>
        private void InitializeOverlayWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlayWindow = new OverlayWindow();
                _overlayWindow.MicrophoneButtonClicked += (s, e) => MicrophoneButtonClicked?.Invoke(this, e);
                _overlayWindow.OptionsButtonClicked += (s, e) => OptionsButtonClicked?.Invoke(this, e);
                _overlayWindow.UserInteracted += (s, e) => ResetHideTimer();

                // Hide initially
                _overlayWindow.Hide();
            });
        }

        public void ShowOverlay(Rect targetPosition, AutomationElement element = null)
        {
            _currentElement = element;
            // Reset the hide timer
            _hideTimer.Change(HideDelayMs, Timeout.Infinite);

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Calculate the position to place the overlay
                Point position = CalculateOverlayPosition(targetPosition);

                // Ensure we don't place it at the exact same position repeatedly
                // (which might cause focus issues)
                if (Math.Abs(position.X - _lastPosition.X) < 5 &&
                    Math.Abs(position.Y - _lastPosition.Y) < 5)
                {
                    // Skip repositioning if the position hasn't changed much
                    if (_isVisible) return;
                }

                _lastPosition = position;

                // Set the position
                _overlayWindow.Left = position.X;
                _overlayWindow.Top = position.Y;

                // Make visible if not already
                if (!_isVisible)
                {
                    _overlayWindow.Show();
                    _isVisible = true;

                    // Make sure it's topmost
                    _overlayWindow.Topmost = true;
                }
            });
        }

        public void HideOverlay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_isVisible)
                {
                    // Stop any ongoing recording when hiding
                    if (_speechService.IsRecognizing)
                    {
                        _speechService.StopRecognition();
                    }
                    _overlayWindow.Hide();
                    _isVisible = false;
                    _currentElement = null;
                }
            });
        }
        private void HideTimerCallback(object state)
        {
            HideOverlay();
        }
        public void ShowRecordingInProgress()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlayWindow.ShowRecordingInProgress();
            });
        }

        public void ShowRecordingStopped()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlayWindow.ShowRecordingStopped();
            });
        }

        private Point CalculateOverlayPosition(Rect targetRect)
        {
            // Default positioning: to the right of the input field
            double x = targetRect.Right + 5;
            double y = targetRect.Top;

            // Check if there's enough room to the right, otherwise position to the left
            if (x + 80 > SystemParameters.VirtualScreenWidth)
            {
                x = targetRect.Left - 80 - 5;

                // If not enough room on either side, position above or below
                if (x < 0)
                {
                    x = targetRect.Left;

                    // Try below the field
                    y = targetRect.Bottom + 5;

                    // If not enough room below, position above
                    if (y + 40 > SystemParameters.VirtualScreenHeight)
                    {
                        y = targetRect.Top - 40 - 5;
                    }
                }
            }

            // Ensure the overlay is within screen bounds
            x = Math.Max(0, Math.Min(x, SystemParameters.VirtualScreenWidth - 80));
            y = Math.Max(0, Math.Min(y, SystemParameters.VirtualScreenHeight - 40));

            return new Point(x, y);
        }

        public AutomationElement GetCurrentElement()
        {
            return _currentElement;
        }
        // Add a method to reset the timer when user interacts with overlay
        public void ResetHideTimer()
        {
            _hideTimer.Change(HideDelayMs, Timeout.Infinite);
        }
        public void Cleanup()
        {
            _speechService?.Dispose();
            _hideTimer?.Dispose();
        }
    }
}