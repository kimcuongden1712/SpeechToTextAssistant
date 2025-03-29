using NAudio.Wave;
using SpeechToTextAssistant.Helpers;
using SpeechToTextAssistant.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using Vosk;

namespace SpeechToTextAssistant.Services
{
    public class SpeechRecognitionService : IDisposable
    {
        private Model _model;
        private VoskRecognizer _recognizer;
        private WaveInEvent _waveIn;
        private bool _isRecognizing = false;
        private bool _isInitialized = false;

        // Events
        public event EventHandler<SpeechRecognitionResult> SpeechRecognized;
        public event EventHandler RecognitionStarted;
        public event EventHandler RecognitionStopped;
        public event EventHandler<string> RecognitionError;
        public event EventHandler<float> AudioLevelChanged;

        public bool IsRecognizing => _isRecognizing;
        public bool IsInitialized => _isInitialized;

        // Thêm các biến để lưu trữ âm thanh đã thu
        private List<byte[]> _recordedAudioBuffers = new List<byte[]>();
        private MemoryStream _audioStream;
        private bool _isCollectingAudio = false;

        // Thêm sự kiện thông báo trạng thái thu âm
        public event EventHandler<bool> RecordingStateChanged;

        // Thuộc tính để kiểm tra trạng thái
        public bool IsCollectingAudio => _isCollectingAudio;

        public SpeechRecognitionService()
        {
            // Khởi tạo sẽ được thực hiện bất đồng bộ thông qua Initialize()
            Initialize();
        }

        /// <summary>
        /// Khởi tạo Vosk Speech Recognition model
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vosk-model-vn");

                if (!Directory.Exists(modelPath))
                {
                    throw new DirectoryNotFoundException($"Không tìm thấy mô hình Vosk tại {modelPath}");
                }

                _model = new Model(modelPath);
                _recognizer = new VoskRecognizer(_model, 16000.0f);
                _recognizer.SetMaxAlternatives(0);
                _recognizer.SetWords(true);
                _recognizer.SetPartialWords(true);

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Không thể khởi tạo nhận dạng giọng nói: {ex.Message}");
            }
        }

        public bool StartRecognition()
        {
            if (!_isInitialized || _isRecognizing) return false;
            // Nếu đang thu âm thì dừng lại và xử lý

            if (_isRecognizing && _isCollectingAudio)
            {
                return ProcessRecordedAudio();
            }
            try
            {
                if (_waveIn != null)
                {
                    _waveIn.Dispose();
                }

                _waveIn = new WaveInEvent
                {
                    DeviceNumber = 0, // Microphone mặc định
                    WaveFormat = new WaveFormat(16000, 1), // 16kHz mono - định dạng Vosk yêu cầu
                    BufferMilliseconds = 50
                };

                _recognizer.Reset();
                _recordedAudioBuffers.Clear();
                _audioStream = new MemoryStream();
                _waveIn.DataAvailable += WaveIn_DataAvailableForRecording;
                _waveIn.RecordingStopped += WaveIn_RecordingStopped;

                _waveIn.StartRecording();
                _isRecognizing = true;
                _isCollectingAudio = true;
                RecordingStateChanged?.Invoke(this, true);
                RecognitionStarted?.Invoke(this, EventArgs.Empty);

                return true;
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Không thể bắt đầu nhận dạng giọng nói: {ex.Message}");
                return false;
            }
        }

        public bool StopRecognition()
        {
            if (!_isRecognizing) return false;

            try
            {
                if (_isCollectingAudio)
                {
                    return ProcessRecordedAudio();
                }
                else
                {
                    _waveIn?.StopRecording();
                    _isRecognizing = false;
                    RecognitionStopped?.Invoke(this, EventArgs.Empty);
                    return true;
                }
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Không thể dừng nhận dạng giọng nói: {ex.Message}");
                return false;
            }
        }

        private void WaveIn_DataAvailableForRecording(object sender, WaveInEventArgs e)
        {
            try
            {
                // Tính toán mức âm lượng để hiển thị trực quan
                CalculateAudioLevel(e.Buffer, e.BytesRecorded);

                // Lưu dữ liệu âm thanh vào bộ nhớ đệm
                byte[] buffer = new byte[e.BytesRecorded];
                Buffer.BlockCopy(e.Buffer, 0, buffer, 0, e.BytesRecorded);
                _recordedAudioBuffers.Add(buffer);
                _audioStream.Write(e.Buffer, 0, e.BytesRecorded);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    RecognitionError?.Invoke(this, $"Lỗi xử lý âm thanh: {ex.Message}"));
            }
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _isRecognizing = false;

            if (e.Exception != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    RecognitionError?.Invoke(this, $"Ghi âm bị dừng do lỗi: {e.Exception.Message}"));
            }

            // Thông báo rằng nhận dạng đã dừng
            Application.Current.Dispatcher.Invoke(() =>
                RecognitionStopped?.Invoke(this, EventArgs.Empty));

            _waveIn?.Dispose();
            _waveIn = null;
        }

        private void CalculateAudioLevel(byte[] buffer, int bytesRecorded)
        {
            // Chuyển đổi byte[] thành short[] (mẫu 16-bit)
            int shortSampleCount = bytesRecorded / 2;
            short[] shortBuffer = new short[shortSampleCount];
            Buffer.BlockCopy(buffer, 0, shortBuffer, 0, bytesRecorded);

            // Tính RMS (root mean square)
            double sum = 0;
            for (int i = 0; i < shortSampleCount; i++)
            {
                sum += Math.Pow(shortBuffer[i], 2);
            }

            double rms = Math.Sqrt(sum / shortSampleCount);

            // Chuyển đổi sang thang 0-1
            float audioLevel = (float)Math.Min(1.0, rms / 32768.0);

            // Thông báo mức âm lượng
            Application.Current.Dispatcher.Invoke(() =>
                AudioLevelChanged?.Invoke(this, audioLevel));
        }

        #region Text Insertion Methods - Unchanged

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
                RecognitionError?.Invoke(this, $"Lỗi chèn văn bản: {ex.Message}");
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
                RecognitionError?.Invoke(this, $"Lỗi thêm văn bản: {ex.Message}");
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
                RecognitionError?.Invoke(this, $"Lỗi mô phỏng bàn phím: {ex.Message}");
            }

            // Khôi phục clipboard
            if (!string.IsNullOrEmpty(currentClipboard))
            {
                System.Windows.Clipboard.SetText(currentClipboard);
            }
        }

        #endregion
        private bool ProcessRecordedAudio()
        {
            try
            {
                // Dừng thu âm
                _waveIn?.StopRecording();
                _isCollectingAudio = false;
                RecordingStateChanged?.Invoke(this, false);

                // Thông báo đang xử lý
                Application.Current.Dispatcher.Invoke(() =>
                    RecognitionStarted?.Invoke(this, EventArgs.Empty));

                // Xử lý dữ liệu đã thu âm
                byte[] audioData = _audioStream.ToArray();
                // Lưu thành WAV file để debug
                string waveFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"speech_recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

                using (var writer = new WaveFileWriter(waveFilePath, new WaveFormat(16000, 1)))
                {
                    writer.Write(audioData, 0, audioData.Length);
                }
                if (audioData.Length == 0)
                {
                    RecognitionError?.Invoke(this, "Không có dữ liệu âm thanh nào được thu thập");
                    return false;
                }

                // Reset recognizer để xử lý toàn bộ đoạn âm thanh thu được
                _recognizer.Reset();

                //var a = RecognizeWavFile(waveFilePath); Debug for dev
                // Xử lý theo từng chunk
                int bufferSize = 4000;
                int offset = 0;
                string intermediateText = null;
                while (offset < audioData.Length)
                {
                    int bytesToProcess = Math.Min(bufferSize, audioData.Length - offset);
                    byte[] chunkBuffer = new byte[bytesToProcess];
                    Buffer.BlockCopy(audioData, offset, chunkBuffer, 0, bytesToProcess);

                    bool accepted = _recognizer.AcceptWaveform(chunkBuffer, bytesToProcess);

                    if (accepted)
                    {
                        var chunkResult = JsonSerializer.Deserialize<VoskResult>(_recognizer.Result());
                        if (!string.IsNullOrEmpty(chunkResult?.Text))
                        {
                            // Lưu lại kết quả trung gian
                            intermediateText = chunkResult.Text;
                            Console.WriteLine($"Intermediate result: {intermediateText}");
                        }
                    }

                    offset += bytesToProcess;
                }

                // Lấy kết quả cuối
                var result = JsonSerializer.Deserialize<VoskResult>(_recognizer.FinalResult());

                // QUAN TRỌNG: Nếu kết quả cuối trống, sử dụng kết quả trung gian
                if (string.IsNullOrEmpty(result?.Text) && !string.IsNullOrEmpty(intermediateText))
                {
                    result = new VoskResult { Text = intermediateText };
                    Console.WriteLine($"Using intermediate result: {result.Text}");
                }

                if (!string.IsNullOrEmpty(result?.Text))
                {
                    Console.WriteLine($"Recognized text: {result.Text}");
                    var recognitionResult = new SpeechRecognitionResult(result.Text);
                    Application.Current.Dispatcher.Invoke(() =>
                        SpeechRecognized?.Invoke(this, recognitionResult));
                }
                else
                {
                    RecognitionError?.Invoke(this, "Không nhận dạng được giọng nói");
                }

                // Dọn dẹp
                _audioStream.Dispose();
                _audioStream = new MemoryStream();
                _recordedAudioBuffers.Clear();
                _isRecognizing = false;

                // Thông báo đã hoàn thành
                RecognitionStopped?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                RecognitionError?.Invoke(this, $"Lỗi khi xử lý âm thanh: {ex.Message}");
                _isRecognizing = false;
                RecognitionStopped?.Invoke(this, EventArgs.Empty);
                return false;
            }
        }
        public void Dispose()
        {
            StopRecognition();
            _waveIn?.Dispose();
            _model?.Dispose();
            _recognizer?.Dispose();
            _audioStream?.Dispose();
        }
        public string RecognizeWavFile(string filePath)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Vosk không được khởi tạo");

            Console.WriteLine($"Processing WAV file: {filePath}");

            // Store the best intermediate result
            string bestIntermediateResult = null;

            using (var waveFile = new AudioFileReader(filePath))
            {
                // Create a new recognizer specifically for this file
                var rec = new VoskRecognizer(_model, 16000.0f);
                rec.SetMaxAlternatives(0);
                rec.SetWords(true);
                rec.SetPartialWords(true); // Add this line

                // Already in right format (assuming file is 16kHz mono)
                byte[] buffer = new byte[4096];
                int bytesRead;
                string lastPartial = null;

                while ((bytesRead = waveFile.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (rec.AcceptWaveform(buffer, bytesRead))
                    {
                        string resultJson = rec.Result();
                        Console.WriteLine($"Intermediate result: {resultJson}");

                        // Save intermediate results
                        var intermediateResult = JsonSerializer.Deserialize<VoskResult>(resultJson);
                        if (!string.IsNullOrEmpty(intermediateResult?.Text))
                        {
                            bestIntermediateResult = intermediateResult.Text;
                        }
                    }
                    else
                    {
                        string partialJson = rec.PartialResult();
                        Console.WriteLine($"Partial: {partialJson}");

                        // Track partial results too
                        var partial = JsonSerializer.Deserialize<VoskPartialResult>(partialJson);
                        if (!string.IsNullOrEmpty(partial?.Partial))
                        {
                            lastPartial = partial.Partial;
                        }
                    }
                }

                // Get final result
                var finalResult = rec.FinalResult();
                Console.WriteLine($"Final result: {finalResult}");

                // If final result is empty but we have intermediate results, use those
                var finalObj = JsonSerializer.Deserialize<VoskResult>(finalResult);
                if (string.IsNullOrEmpty(finalObj?.Text))
                {
                    if (!string.IsNullOrEmpty(bestIntermediateResult))
                    {
                        Console.WriteLine($"Using best intermediate result: {bestIntermediateResult}");
                        return JsonSerializer.Serialize(new VoskResult { Text = bestIntermediateResult });
                    }
                    else if (!string.IsNullOrEmpty(lastPartial))
                    {
                        Console.WriteLine($"Using last partial result: {lastPartial}");
                        return JsonSerializer.Serialize(new VoskResult { Text = lastPartial });
                    }
                }

                return finalResult;
            }
        }

    }
}