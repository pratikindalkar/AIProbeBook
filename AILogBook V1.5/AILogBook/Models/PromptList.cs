using System.ComponentModel.DataAnnotations;

namespace AILogBook.Models
{
    public class PromptList
    {
        public int AutoID { get; set; }
        public string InitialPrompt { get; set; }
        public string AIName { get; set; }
        public int CategoryID { get; set; }
        public int TopicID { get; set; }
        public string Prompt1 { get; set; }
        public string Prompt2 { get; set; }
        public string Prompt3 { get; set; }
        public string PositiveAttributes { get; set; }
        public string NegativeAttributes { get; set; }
        public string MinLength { get; set; }
        public string MaxLength { get; set; }
        public string MinPromptText { get; set; }
        public string MaxPromptText { get; set; }
        public string CharCountPrompt { get; set; }
        public string WordCountPrompt { get; set; }
        public string EndPrompt { get; set; }
        public string FinalEndPrompt { get; set; }
        public string MergePrompt { get; set; }
        public string FinalPrompt { get; set; }
        public string Rating { get; set; }
        public string UpdatedUser { get; set; }
        public string UpdatedDate { get; set; }
        public string UpdatedTime { get; set; }
        public bool Active { get; set; }
        public string? CategoryName { get; set; }
        public string? ShortTopic { get; set; }
        public string? Count { get; set; }
        public string BlockedPrompt { get; set; }
        public string UserIgnorePrompt { get; set; }
        public string? EnglishLanguagePrompt { get; set; }
        public string? OtherLanguagePrompt { get; set; }
        public string? PromptCategory { get; set; }

    }
}
