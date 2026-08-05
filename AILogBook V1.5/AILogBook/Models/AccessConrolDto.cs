namespace AILogBook.Models
{
    public class AccessConrolDto
    {
        public string? Login_Name { get; set; }
        public int Auto_Id { get; set; }
        public string? Section { get; set; }
        public string? Type { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Update { get; set; }
        public bool Delete { get; set; }
        public bool Download { get; set; }
        public bool M_active { get; set; }
        public bool active { get; set; }
        public string? DBDetails { get; set; }
        public string? DBProfile { get; set; }
        public string? multiuserflag { get; set; }
        public string? securitykey { get; set; }
        public string? securityvalue { get; set; }
        public string? sqltimeout { get; set; }
        public string? rollbackcommit { get; set; }
        public bool encrypt { get; set; }
        public string[]? SQLStatements { get; set; }
        public string[]? SQLReturntype { get; set; }
    }
}
