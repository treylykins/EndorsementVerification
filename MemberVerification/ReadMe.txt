This Member Verification program runs on the command line and has the following assumptions:

1. A file called "membership.xlsx" with the following worksheets:
	A. "members": List of current members
    B. "aliases": List of known member aliases (ex. Dave for someone who registered as David)
    C. "outsiders": List of members outside the district
    D. "late":  List of members who missed the endorsements meeting registration deadline defined in the bylaws
    E.  All of the above worksheets are expected to be formatted as a single column of names in Column A of an xlsx workbook
2. A file called "votes.xlsx" with the following worksheets:
    A. "manual": List of manual votes
        i. Formatting:
            a. Row 1 contains the names of each ballot starting on Column B and continuing to the end
            b. Row 2 is a header row that is not imported
            c. Rows 3+ list the voter names in Column A and their selection under the appropriate column heading B-to-the-end.
            d. Ex. If Column B is the slate and Column C is Mayor, Row 3 would have "Jane Voter" in Column A, "Yes" in Column B, and "Mayoral Candidate X" in Column C
    B. "zoom": Copy of poll results downloaded from Zoom.
        i. Formatting is expected to match Zoom poll results as they exist in 2022.
        ii. 6 rows of header info that is not imported
        iii. Rows 7+ contain the following columns
            a. Id
            b. User Name
            c. User Email
            d. Submitted Date/Time (not used)
            e. Poll title
            f. Selection