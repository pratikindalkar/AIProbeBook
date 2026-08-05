namespace AILogBook.Models
{
    public class UserRequest
    {
        public List<ChatMessage> Messages { get; set; }
        public string Model { get; set; }
    }
}
