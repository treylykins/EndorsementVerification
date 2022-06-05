using System.Collections.Generic;

namespace MemberVerification.Models
{
    public class Membership
    {
        public List<string> Members { get; set; }
        public List<string> Aliases { get; set; }
        public List<string> OutOfDistrictMembers { get; set; }
        public List<string> MissedDeadline { get; set; }
    }
}
