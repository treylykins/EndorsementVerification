using System;
using System.Collections.Generic;
using System.Text;

namespace MemberVerification.Models
{
    public class Results
    {
        public List<Vote> Votes { get; set; } = new List<Vote>();
        public List<Vote> Duplicates { get; set; } = new List<Vote>();
        public List<Suspect> Suspects { get; set;} = new List<Suspect>();
    }
}
