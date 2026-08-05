namespace AILogBook.Models
{
    public class Topics
    {
        public int AutoId { get; set; }
        public string Topic { get; set; }
        public string ShortTopic { get; set; }
        public string UpdatedUser { get; set; }
        public string UpdatedDate { get; set; }
        public string UpdatedTime { get; set; }
        public bool Active { get; set; }
    }
}
