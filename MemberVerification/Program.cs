using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MemberVerification
{
    class Program
    {
        private static string _membershipFile { get; set; }
        private static string _pollResultsFile { get; set; }
        private static string _outputFile { get; set; }
        private static string _aliasFile { get; set; }
        private static List<string> _supsectList { get; set; } = new List<string>();
        private static string _manualVotesFile { get; set; }
        private static string _outOfDistrictFile { get; set; }
        private static string _missedRegistrationDeadline { get; set; }

        /// <summary>
        /// Compares membership list with poll results and outputs a file of suspected invalid votes
        /// The output will need to be compared with the membership list manually because it may contain false positives
        /// Such as:  Typos, unknown aliases (Bob != Robert), name changes, extra text in poll name (such as a title or gender pronouns), etc
        /// </summary>
        /// <param name="args[0]">Membership.csv file</param>
        /// <param name="args[1]">Poll Results.csv file</param>
        /// <param name="args[2]">Output.csv file</param>
        /// <param name="args[3]">Known Aliases.csv file</param>
        /// <param name="args[4]">Manual entries.csv file</param>
        /// <param name="args[5]">Out of District.csv</param>
        /// <param name="args[6]">Missed Deadline.csv</param>
        public static void Main(string[] args)
        {
            processArgs(args);
            checkForDuplicateOutputFiles();
            
            var memberList = ReadFileToList(_membershipFile);
            memberList.AddRange(ReadFileToList(_aliasFile));

            var rawVoteData = parseVotersAndVotes(ReadFileToList(_pollResultsFile));
            var manualVotes = parseVotersAndVotes(ReadFileToList(_manualVotesFile));
            var pollParticipantList = processVotes(rawVoteData);
            pollParticipantList.AddRange(processVotes(manualVotes));

            pollParticipantList.Sort();

            verifyPollResults(memberList, pollParticipantList);
            

            var calculation = calculateVotes(rawVoteData, _supsectList);
            
            writeResults(rawVoteData, calculation, pollParticipantList);

            Console.WriteLine("Verification complete.");
            Console.WriteLine("Please see the output file for suspected invalid votes");
        }

        private static Dictionary<string, int> calculateVotes(List<(string, string)> rawVoteData, List<string> supsectList)
        {
            var results = new Dictionary<string, int>();
            foreach (var data in rawVoteData)
            {
                if (supsectList.Contains(data.Item1))
                {
                    // is vote already listed?
                    if (results.ContainsKey(data.Item2))
                    {
                        results[data.Item2]++;
                    }
                    else
                    {
                        results[data.Item2] = 1;
                    }
                }
            }

            return results;
        }

        private static List<string> processVotes(List<(string, string)> rawVoteData)
        {
            var names = new List<string>();
            foreach (var name in rawVoteData)
            {
                names.Add(name.Item1);
            }
            return names;

        }

        private static List<(string, string)> parseVotersAndVotes(List<string> pollParticipantList)
        {
            var votes = new List<(string Name, string Vote)>();
            foreach (var line in pollParticipantList)
            {
                var split = line.Split(',');    
                var name = split[1];
                var response = split[5];
                
                votes.Add((name, response));
            }

            votes.Sort();

            return votes;
        }

        private static List<string> flagMissedDeadlineVotes(List<string> pollParticipantList)
        {
            var slackers = new List<string>();
            if (string.IsNullOrEmpty(_missedRegistrationDeadline)) return slackers;

            var missedDeadlineList = ReadFileToList(_missedRegistrationDeadline);
            

            foreach (var lateRegistrant in missedDeadlineList)
            {
                var result = pollParticipantList.FindIndex(x => x.Equals(lateRegistrant, StringComparison.OrdinalIgnoreCase));
                if (result != -1)
                {
                    slackers.Add($"{lateRegistrant} missed registration deadline");
                }
            }

            return slackers;
        }

        private static List<string> flagOutOfDistrictVotes(List<string> pollParticipantList)
        {
            var outsiders = new List<string>();
            if (string.IsNullOrEmpty(_outOfDistrictFile)) return outsiders;

            var outOfDistrictList = ReadFileToList(_outOfDistrictFile);
            

            foreach (var outsider in outOfDistrictList)
            {
                var result = pollParticipantList.FindIndex(x => x.Equals(outsider, StringComparison.OrdinalIgnoreCase));
                if (result != -1)
                {
                    outsiders.Add($"{outsider} lives outside the 34th");
                }
            }
            return outsiders;

        }

        private static List<string> flagMultipleVotes(List<string> pollParticipantList)
        {
            var multiples = new List<string>();
            Dictionary<string, int> numberFrequency = pollParticipantList
                                                        .GroupBy(n => n)
                                                        .ToDictionary(g => g.Key, g => g.Count());
            foreach (KeyValuePair<string, int> kv in numberFrequency)
            {
                if (kv.Value > 1)
                {
                    multiples.Add($"{kv.Key} voted {kv.Value} times");
                }
            }

            return multiples;
        }

        private static void processArgs(string[] args)
        {
            if (args.Length < 2)
            {
                throw new Exception();
            }

            _membershipFile = args[0];
            _pollResultsFile = args[1];
            _outputFile = args.Length > 2 ? args[2] : @"\suspectVotes.csv";
            _aliasFile = args.Length > 3 ? args[3] : "";
            _manualVotesFile = args.Length > 4 ? args[4] : "";
            _outOfDistrictFile = args.Length > 5 ? args[5] : "";
            _missedRegistrationDeadline = args.Length > 6 ? args[6] : "";
        }

        private static void checkForDuplicateOutputFiles()
        {
            if (File.Exists(_outputFile))
            {
                _outputFile = _outputFile.Replace(".csv", $"{DateTime.Now.Ticks}.csv");
            }
        }

        private static List<string> ReadFileToList(string filename)
        {
            var list = new List<string>();

            try 
            {
                if (File.Exists(filename))
                {
                    using (var reader = new StreamReader(filename))
                    {
                        while (!reader.EndOfStream)
                        {
                            list.Add(reader.ReadLine());
                        }
                    }
                    list.Sort();
                }
            }
            catch (IOException ex)
            {
                Console.Write($"{filename} is already in use.  Please close and try again");
                throw ex;
            }


            return list;
        }

        private static void verifyPollResults(List<string> memberList, List<string> pollParticipantList)
        {
            foreach (var voter in pollParticipantList)
            {
                var result = memberList.FindIndex(x => x.Equals(voter, StringComparison.OrdinalIgnoreCase));
                if (result == -1)
                {
                    _supsectList.Add(voter);
                }
            }

            if (_supsectList.Count == 0)
            {
                _supsectList.Add("No votes were found belonging to non-members");
            }
        }

        private static void writeResults(List<(string, string)> rawVoteData, Dictionary<string, int> calculation, List<string> pollParticipants)
        {
            using (var writer = new StreamWriter(_outputFile))
            {
                writer.WriteLine("**Calculated bad votes**");
                foreach (var key in calculation.Keys)
                {
                    writer.WriteLine($"{key},{calculation[key]}");
                }

                writer.WriteLine();
                writer.WriteLine("**Suspect votes**");
                foreach (var rawVote in rawVoteData)
                {
                    if (_supsectList.Contains(rawVote.Item1))
                    {
                        writer.WriteLine($"{rawVote.Item1},{rawVote.Item2}");
                    }
                }

                writer.WriteLine();
                writer.WriteLine("**Problematic voters**");
                var multiples = flagMultipleVotes(pollParticipants);

                if (multiples.Any())
                {
                    foreach (var multiple in multiples)
                    {
                        writer.WriteLine(multiple);
                    }
                }

                var outsiders = flagOutOfDistrictVotes(pollParticipants);
                if (outsiders.Any())
                {
                    foreach (var outsider in outsiders)
                    {
                        writer.WriteLine(outsider);
                    }
                }

                var slackers = flagMissedDeadlineVotes(pollParticipants);
                if (slackers.Any())
                {
                    foreach (var slacker in slackers)
                    {
                        writer.WriteLine(slacker);
                    }
                }
            }
        }
    }
}
