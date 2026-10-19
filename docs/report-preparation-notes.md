# Report preparation notes

The supplied final technical report brief requests approximately 2,000 words, allocated as approximately 1,500 for language analysis and 500 for the application discussion. Tables, screenshots, large code blocks and references are excluded. No page cap appears in the supplied brief. Its wording refers once to the preliminary report, but the Part A and Part B allocations and final-report rubric clearly define this task.

The final DOCX contains 13 pages with placeholders, including its cover, references and appendices. A conservative count of prose after the cover, including headings, captions, placeholder labels and appendix notes, is 2,191 words. The main discussion contains 1,864 words: 1,365 in Part A and 499 in Part B. Counts are based on whitespace-separated tokens; Word may report a slightly different total. Large code and Mermaid blocks, tables and the reference list are excluded from these reported counts.

The narrative was kept concise to allow for caption and appendix wording. When diagrams and screenshots are inserted, pagination changes. The brief does not establish a 10% tolerance or a fixed maximum, so no unstated exemption or numerical page rule is claimed.

The source matrix in the report evaluates currency, relevance, authority, accuracy and purpose. The supplied 2026 lecture slides establish the unit's conceptual framing. Microsoft and Oracle language specifications resolve exact semantics; official platform documentation supports framework and concurrency APIs. Java SE 25 is a deliberate versioned comparison, not a claim to describe the newest Java release. Old wording in lecture slides is not treated as authority for current language-specific details.

## Rubric mapping

| Requirement | Report evidence |
| --- | --- |
| Naming and types | Sections 1 and 2; typed model and amount validation in A1 |
| Expressions, assignment and control | Sections 3 and 4; validation, switch and LINQ examples |
| Subprograms and ADTs | Sections 5 and 6; Figure 1, A3 and A5 |
| Object-oriented, functional and structured paradigms | Section 7; repository abstraction, class diagram and A2 |
| Concurrency and parallelism | Section 8; repository semaphore in A4 and concurrent-save check |
| Exceptions and events | Section 9; Figure 3, A4 and budget-event check |
| Similar-language comparison | Section 10 and Table 1 |
| Readability, writability and performance | Section 11; balanced discussion without fabricated benchmarks |
| Demonstration and final design | Sections 12 and 13; Figures 2 and 3; S1 to S5 |
| Two good aspects | Section 14; monetary typing and LINQ, A1 and A2 |
| Two bad aspects | Section 15; mutable UI notification verbosity and unchecked exception effects, A3 and A4 |
| Illustrative examples | Selected source, 3 Mermaid diagrams and 5 screenshot placeholders |
| Source reliability | References and CRAAP assessment matrix |

## Evidence still to complete

Run the desktop application on your development machine, insert your own screenshots in S1 to S4, and replace S5 with your terminal run. Render and insert Figures 1 to 3 above their Mermaid source. Retain a backup of the DOCX before replacing placeholders. The README gives exact data and click sequences.

The build log records a successful Linux Release compilation with zero warnings and errors. The verification log records 33 of 33 checks passed. No native desktop capture, macOS run, deployment or GitHub Actions execution is claimed. The workflow is prepared but has not been pushed or executed remotely.

The report is grounded in the accompanying implementation rather than invented personal development experience. Review the source, run it yourself and describe only observations you can reproduce. Follow the unit's applicable rules for acknowledging assistance; this package does not include a sole-authorship declaration.
