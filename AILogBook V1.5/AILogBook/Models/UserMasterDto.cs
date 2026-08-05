namespace AILogBook.Models
{
    public class UserMasterDto
    {
        public string? Module { get; set; }
        public string? Category { get; set; }
        public int Topic_id { get; set; }
        public int Session_created { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
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
        public int Auto_Id { get; set; }
        public string? Login_Name { get; set; }
        public string? User_Password { get; set; }
        public string? Type { get; set; }
        public bool active { get; set; }
        public int Level { get; set; }
    }
}
