namespace AILogBook.Models
{
    public class SurveyResponses
    {
        public int AutoId { get; set; }
        public string Respondent_Id { get; set; }
        public string ProjectKey { get; set; }
        public string QName { get; set; }
        public string JsonData { get; set; }
        public bool Active { get; set; }
    }
}
