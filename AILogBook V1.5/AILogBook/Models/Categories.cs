namespace AILogBook.Models
{
    public class Categories
    {
        public int AutoId { get; set; }
        public string CategoryName { get; set; }
        public string UpdatedUser { get; set; }
        public string UpdatedDate { get; set; }
        public string UpdatedTime { get; set; }
        public bool Active { get; set; }
        public string? count { get; set; }
    }
}
