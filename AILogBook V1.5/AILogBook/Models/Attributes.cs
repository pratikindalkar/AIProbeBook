using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AILogBook.Models
{
    public class Attributes
    {
        public int AutoId { get; set; }
        public int CategoryId { get; set; }
        public string Attribute { get; set; }
        public string Type { get; set; }
        public string UpdatedUser { get; set; }
        public string UpdatedDate { get; set; }
        public string UpdatedTime { get; set; }
        public bool Active { get; set; }
        public string? CategoryName { get; set; }
    }
}
