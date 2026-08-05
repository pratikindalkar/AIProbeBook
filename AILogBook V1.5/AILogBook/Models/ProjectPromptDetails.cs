namespace AILogBook.Models
{
    public class ProjectPromptDetails
    {
        public int AutoId { get; set; }
        public string Project_Code { get; set; }
        public string ProjectKey { get; set; }
        public string AIName { get; set; }
        public int Model_Id { get; set; }
        public string FinalEndPrompt { get; set; }
        public string FinalPrompt { get; set; }
        public string CategoryName { get; set; }
        public string ShortTopic { get; set; }
        public bool Active { get; set; }
    }
}
