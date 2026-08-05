namespace AILogBook.Models
{
    public class Projects
    {
        public int AutoId { get; set; }
        public int Prompt_Id { get; set; }
        public int Model_Id { get; set; }
        public string ProjectKey { get; set; }
        public string From_Date { get; set; }
        public string To_Date { get; set; }
        public string Project_Code { get; set; }
        public bool Active { get; set; }
        public string? ModelName { get; set; }
        public string? PromptCategory { get; set; }
    }
}
