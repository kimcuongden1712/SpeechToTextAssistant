using System.Text.Json.Serialization;
namespace SpeechToTextAssistant.Models
{
    public class VoskPartialResult
    {
        [JsonPropertyName("partial")]
        public string Partial { get; set; }
    }
}
