# Contributing

Thanks for contributing to Db2.HealthChecks.

## Local validation

Run before opening a pull request:

```bash
dotnet restore
dotnet build
dotnet test
dotnet pack src/Db2.HealthChecks/Db2.HealthChecks.csproj -c Release
```

## Guidelines

- Keep public API changes backward compatible where possible.
- Add or update tests for behavior changes.
- Do not commit credentials, connection strings, generated packages, or local artifacts.
- Follow semantic versioning for public API changes.
