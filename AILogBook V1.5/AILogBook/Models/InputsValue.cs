namespace AILogBook.Models
{
    public class InputsValue
    {
        public string[] SQLStatements { get; set; }
        public string[] SQLReturntype { get; set; }
        public string DBDetails { get; set; }
        public string DBProfile { get; set; }
        public string sqltimeout { get; set; }
        public string multiuserflag { get; set; }
        public string securitykey { get; set; }
        public string securityvalue { get; set; }
        public string rollbackcommit { get; set; }
        public bool encrypt { get; set; }
    }
}
