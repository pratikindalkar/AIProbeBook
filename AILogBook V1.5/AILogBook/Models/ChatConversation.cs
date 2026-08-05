namespace AILogBook.Models
{
    public class ChatConversation
    {
        public int AutoId { get; set; }
        public int ChatSession_id { get; set; }
        public int Chat_id { get; set; }
        public int? Prompt_id { get; set; }
        public int Respondent_id { get; set; }
        public string Question { get; set; }
        public string? ProjectCode { get; set; }
        public string UserResponse { get; set; }
        public string Category { get; set; }
        public string Topic { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedTime { get; set; }
        public string ModelId { get; set; }
        public int QuestionWordCount { get; set; }
        public int QuestionCharCount { get; set; }
        public int ResponseWordCount { get; set; }
        public int ResponseCharCount { get; set; }
    }
}
