# Security Policy

## Supported versions

Security fixes are provided for the latest released minor version.

## Reporting a vulnerability

Please report security issues privately through GitHub Security Advisories when available, or contact the maintainer directly.

Do not open public issues containing credentials, connection strings, stack traces with secrets, or exploit details.

## Operational guidance

- Never expose health check details publicly unless authenticated and authorized.
- Do not log Db2 connection strings or credentials.
- Prefer secret stores such as Azure Key Vault, Kubernetes Secrets, or equivalent enterprise secret managers.
- Use short health check timeouts to avoid cascading failures.
