# Security Policy

## Reporting a vulnerability

Please report security issues privately through GitHub: open the **Security** tab of this repository and choose
**Report a vulnerability**. Do not open a public issue.

You can expect an acknowledgement within a few working days.

## Scope

This is a local command-line tool. It reads CSV and JSON files from a folder you choose and writes one CSV report;
it opens no network connections and stores no credentials. Relevant issues include crafted input files that
crash the tool in an unhandled way, read or write outside the given paths, or produce a report that runs code
when opened in a spreadsheet.

## Data

All data in this repository is synthetic. Do not attach real company or personal data to a report.
