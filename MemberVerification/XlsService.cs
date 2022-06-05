using MemberVerification.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MemberVerification
{
    public class XlsService
    {
        private string _membershipFilePath { get; set; }
        private string _voteFilePath { get; set; }
        public XlsService(string membershipFilePath, string voteFilePath)
        {
            if (!File.Exists(membershipFilePath))
            {
                throw new FileNotFoundException();
            }
            if (!File.Exists(voteFilePath))
            {
                throw new FileNotFoundException();
            }
            _membershipFilePath = membershipFilePath;
            _voteFilePath = voteFilePath;
        }

        //private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public List<string> ReadOneColumnWorksheet(string worksheetName)
        {
            var list = new List<string>();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(_membershipFilePath))
            {
                var sheet = package.Workbook.Worksheets[worksheetName];

                for (var i = 1; i < sheet.Rows.EndRow; i++)
                {
                    var val = sheet.Cells[i, 1].Value;
                    if (val == null) { break; }
                    list.Add(val.ToString());
                }

                list.Sort();
                return list;
            }
        }

        public List<Vote> ReadManualVotes(string worksheetName)
        {
            var votes = new List<Vote>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(_voteFilePath))
            {
                var sheet = package.Workbook.Worksheets[worksheetName];
                for (var i = 3; i < sheet.Rows.EndRow; i++)
                {
                    var voter = sheet.Cells[i, 1].Value?.ToString();
                    if (voter == null) { break; }

                    for (var j = 2; j < sheet.Columns.EndColumn; j++)
                    {
                        var selection = sheet.Cells[i, j].Value?.ToString();
                        if (selection == null) { break; }

                        var vote = new Vote()
                        {
                            Voter = voter,
                            Selection = selection,
                            Ballot = sheet.Cells[1, j].Value?.ToString()
                        };
                        votes.Add(vote);
                    }
                }

            }
            return votes;
        }

        public Results ReadZoomVotes(string worksheetName, List<Vote> manualVotes)
        {
            var votes = manualVotes;
            var dupes = new List<Vote>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(_voteFilePath))
            {
                var sheet = package.Workbook.Worksheets[worksheetName];

                for (var i = 7; i < sheet.Rows.EndRow; i++)
                {
                    var voter = sheet.Cells[i, 2].Value?.ToString();
                    if (voter == null) { break; }
                    var ballot = sheet.Cells[i, 5].Value?.ToString();

                    var existing = votes.Exists(x => x.Voter == voter && x.Ballot == ballot);

                    var vote = new Vote
                    {
                        Id = Convert.ToInt32(sheet.Cells[i, 1].Value),
                        Voter = voter,
                        Email = sheet.Cells[i, 3].Value?.ToString(),
                        Ballot = ballot,
                        Selection = sheet.Cells[i, 6].Value?.ToString()
                    };
                    if (existing)
                    {
                        dupes.Add(vote);
                    }
                    else
                    {
                        votes.Add(vote);
                    }

                }

                var results = new Results
                {
                    Votes = votes.OrderBy(x => x.Voter).ToList(),
                    Duplicates = dupes.OrderBy(x => x.Voter).ToList()
                };

                return results;
            }
        }

        public void WriteResults(Results results, string outputFileName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var suspectWorksheet = package.Workbook.Worksheets.Add("suspects");
                int row;
                // headers
                suspectWorksheet.Cells[1, 1].Value = "Voter";
                suspectWorksheet.Cells[1, 2].Value = "Ballot";
                suspectWorksheet.Cells[1, 3].Value = "Selection";
                suspectWorksheet.Cells[1, 4].Value = "Reason";

                // suspects
                for (var i = 0; i < results.Suspects.Count; i++)
                {
                    row = i + 2;
                    suspectWorksheet.Cells[row, 1].Value = results.Suspects[i].Voter;
                    suspectWorksheet.Cells[row, 2].Value = results.Suspects[i].Ballot;
                    suspectWorksheet.Cells[row, 3].Value = results.Suspects[i].Selection;
                    suspectWorksheet.Cells[row, 4].Value = results.Suspects[i].Reason;
                }
                suspectWorksheet.Cells.AutoFitColumns(0);

                var tallyWorksheet = package.Workbook.Worksheets.Add("tally");

                var ballots = results.Votes.Select(x => x.Ballot).Distinct().ToList();
                row = 0;
                for (var i = 0; i < ballots.Count(); i++)
                {
                    row++;
                    tallyWorksheet.Cells[row, 1].Value = "Ballot:";
                    tallyWorksheet.Cells[row, 2].Value = ballots[i];
                    row++; // spacer

                    var ballotVotes = results.Votes.Where(x => x.Ballot == ballots[i]);
                    var selections = ballotVotes.Select(x => x.Selection).Distinct().ToList();

                    for (var j = 0; j < selections.Count(); j++)
                    {
                        var selectionVotes = ballotVotes.Where(x => x.Selection == selections[j]);
                        row++;
                        tallyWorksheet.Cells[row, 1].Value = selections[j];
                        tallyWorksheet.Cells[row, 2].Value = "Sub Total";
                        tallyWorksheet.Cells[row, 3].Value = selectionVotes.Count();

                        var disqualified = results.Suspects.Where(x => x.Selection == selections[j] && x.Ballot == ballots[i]);
                        if (disqualified.Count() > 0)
                        {
                            tallyWorksheet.Cells[row, 4].Value = "Suspect votes";
                            tallyWorksheet.Cells[row, 5].Value = disqualified.Count();
                        }

                    }
                    row++;
                }
                tallyWorksheet.Cells.AutoFitColumns(0);

                package.SaveAs(outputFileName);
            }
        }
    }
}
