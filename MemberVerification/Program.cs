using MemberVerification.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MemberVerification
{
    class Program
    {
        private static XlsService _xlsService = new XlsService("membership.xlsx", "votes.xlsx");
        private static Membership _membership = new Membership();
        private static Results _results = new Results();
        private static string _outputFile { get; set; } = "results.xlsx";

        /// <summary>
        /// Compares membership list with poll results and outputs a file of suspected invalid votes
        /// The output will need to be compared with the membership list manually because it may contain false positives
        /// Such as:  Typos, unknown aliases (Bob != Robert), name changes, extra text in poll name (such as a title or pronouns), etc
        /// </summary>
        public static void Main(string[] args)
        {
            ReadMembershipFile();
            CheckForDuplicateOutputFiles();
            ReadVoteFile();

            VerifyMembership();
            VerifyRegistrationDeadline();
            VerifyInDistrict();

            _xlsService.WriteResults(_results, _outputFile);
        }

        private static void VerifyRegistrationDeadline()
        {
            foreach (var vote in _results.Votes)
            {
                var result = _membership.MissedDeadline.FindIndex(x => x.Equals(vote.Voter, StringComparison.OrdinalIgnoreCase));
                if (result != -1)
                {
                    _results.Suspects.Add(new Suspect
                    {
                        Id = vote.Id,
                        Voter = vote.Voter,
                        Ballot = vote.Ballot,
                        Selection = vote.Selection,
                        Reason = "Missed registration deadline"
                    });
                }
            }
        }

        private static void VerifyInDistrict()
        {
            foreach (var vote in _results.Votes)
            {
                var result = _membership.OutOfDistrictMembers.FindIndex(x => x.Equals(vote.Voter, StringComparison.OrdinalIgnoreCase));
                if (result != -1)
                {
                    _results.Suspects.Add(new Suspect
                    {
                        Id = vote.Id,
                        Voter = vote.Voter,
                        Ballot = vote.Ballot,
                        Selection = vote.Selection,
                        Reason = "Lives outside district"
                    });
                }
            }
        }

        private static void ReadVoteFile()
        {
            var manualVotes = _xlsService.ReadManualVotes("manual");
            _results = _xlsService.ReadZoomVotes("zoom", manualVotes);
        }

        private static void ReadMembershipFile()
        {
            _membership.Members = _xlsService.ReadOneColumnWorksheet("members");
            _membership.Aliases = _xlsService.ReadOneColumnWorksheet("aliases");
            _membership.OutOfDistrictMembers = _xlsService.ReadOneColumnWorksheet("outsiders");
            _membership.MissedDeadline = _xlsService.ReadOneColumnWorksheet("late");
        }


        private static void CheckForDuplicateOutputFiles()
        {
            if (File.Exists(_outputFile))
            {
                _outputFile = _outputFile.Replace(".csv", $"_{DateTime.Now.Ticks}.csv");
            }
        }

        private static void VerifyMembership()
        {
            var fullMembership = new List<string>();
            fullMembership.AddRange(_membership.Members);
            fullMembership.AddRange(_membership.Aliases);

            foreach (var vote in _results.Votes)
            {
                var result = fullMembership.FindIndex(x => x.Equals(vote.Voter, StringComparison.OrdinalIgnoreCase));
                if (result == -1)
                {
                    _results.Suspects.Add(new Suspect 
                    { 
                        Id = vote.Id,
                        Voter = vote.Voter,
                        Ballot = vote.Ballot,
                        Selection = vote.Selection,
                        Reason = "Not a member"
                    });
                }
            }
        }

    }
}
