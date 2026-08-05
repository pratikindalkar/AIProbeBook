using AILogBook.Models;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace AILogBook.Services
{
    public class ChatService
    {
        private readonly HttpClient httpClient;
        //private const string ChatAPIUrl = "https://localhost:7033/api/chat/ask";
        private const string ChatAPIUrl = "https://surveyxan.com/cloudapp/app36/ChatAPI/api/chat/ask";

        public ChatService(HttpClient _httpClient)
        {
            httpClient = _httpClient;
        }

        public async Task<string> GetBotResponse(List<ChatMessage> history, string ModelId)
        {
            var requestBody = new UserRequest
            {
                Messages = history,
                Model = ModelId
            };

            var response = await httpClient.PostAsJsonAsync(ChatAPIUrl, requestBody);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<BotResult>();
                if (data != null && !string.IsNullOrEmpty(data.reply))
                {
                    return data.reply;
                }
            }
            return "Error: Could not get a response from the AI.";
        }
    }
}
