using static AILogBook.Controllers.AiChatController;

namespace AILogBook.Models
{
    public class UserResponseRequest
    {
        public string response { get; set; }
        public List<HistoryDto> history { get; set; }
    }
}
