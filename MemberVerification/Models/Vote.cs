namespace MemberVerification.Models
{
    public class Vote
    {
        public int Id { get; set; }
        public string Voter { get; set; }
        public string Selection { get; set; }
        public string Email { get; set; }
        public string Ballot { get; set; }
    }
}
