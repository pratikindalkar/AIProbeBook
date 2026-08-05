namespace AILogBook.Models
{
    public class PromptTemplate
    {
        public int AutoId { get;set; }
        public int PromptId { get;set; }
        public string PName { get; set; }
        public string Message { get; set; }
        public string UpdatedUser { get; set; }
        public string UpdatedDate { get; set; }
        public string UpdatedTime { get; set; }
        public bool Active { get; set; }
    }
}
