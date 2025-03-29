using System.Text.Json.Serialization;
namespace SpeechToTextAssistant.Models
{
    public class VoskResult
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
