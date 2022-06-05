using System;
using System.Collections.Generic;
using System.Text;

namespace MemberVerification.Models
{
    public class Suspect : Vote
    {
        public string Reason { get; set; }
    }
}
