# Security Policy

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability. Report it privately to the repository
maintainers through GitHub security advisories.

Include the affected version, target framework, provider package and a minimal reproduction when
possible. Do not include real connection strings, passwords, tokens or production data.

Health-check endpoints should normally be protected and should not expose exception details. The
library defaults `IncludeExceptionDetails` to `false` for this reason.
