namespace SpeechToTextAssistant.Models
{
    public class SpeechRecognitionResult
    {
        public string RecognizedText { get; set; }
        public float Confidence { get; set; } = 1.0f;

        public SpeechRecognitionResult() { }

        public SpeechRecognitionResult(string recognizedText)
        {
            RecognizedText = recognizedText;
            //Confidence = confidence;
        }
    }
}