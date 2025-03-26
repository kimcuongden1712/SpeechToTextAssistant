namespace SpeechToTextAssistant.Models
{
    public class SpeechRecognitionResult
    {
        public string RecognizedText { get; set; }
        public float Confidence { get; set; }

        public SpeechRecognitionResult(string recognizedText, float confidence)
        {
            RecognizedText = recognizedText;
            Confidence = confidence;
        }
    }
}