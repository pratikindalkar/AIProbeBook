using DocumentFormat.OpenXml.Office.CoverPageProps;

namespace AILogBook.Models
{
    public class ChatSession
    {
        public int AutoId { get;set; }
        public int Respondent_Id { get; set; }
        public string ProjectCode { get; set; }
        public string SurveySessionId { get; set; }
        public string Start_Time { get; set; }
        public string  End_Time { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public string Topic { get; set; }
        public int? Prompt_Id { get; set; }
        public int? Rating { get; set; }
        public string FinalPrompt { get; set; }
        public string Usable { get; set; }
        public string SelfEnded { get; set; }
        public string Remarks { get; set; }
        public int? Model_Id { get; set; }
        public int TotalWordCount { get; set; }
        public int TotalCharCount { get; set; }  
        public string? Questions { get; set; } 
        public string? Responses { get; set; }
    }
}
