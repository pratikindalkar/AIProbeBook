namespace ChatAPI.Models
{
    public class OpenAiResponse
    {
        public List<Choice> Choices { get; set; }
        public class Choice { public ChatMessage Message { get; set; } }
    }
}
