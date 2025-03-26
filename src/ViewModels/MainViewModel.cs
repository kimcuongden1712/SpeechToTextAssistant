// - Triển khai INotifyPropertyChanged
// - Quản lý trạng thái ứng dụng
// - Kết nối các services
// - Cung cấp commands cho giao diện người dùng
using SpeechToTextAssistant.Infrastructures;
using System.Windows.Input;

namespace SpeechToTextAssistant.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _recognizedText;
        private bool _isListening;

        public string RecognizedText
        {
            get => _recognizedText;
            set
            {
                _recognizedText = value;
                RaisePropertyChanged();
            }
        }

        public bool IsListening
        {
            get => _isListening;
            set
            {
                _isListening = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ListenCommand));
            }
        }

        public ICommand ListenCommand { get; }

        public MainViewModel()
        {
            ListenCommand = new RelayCommand(OnListen);
        }

        private void OnListen()
        {
            IsListening = !IsListening;
            // Logic to start or stop speech recognition
        }
    }

}