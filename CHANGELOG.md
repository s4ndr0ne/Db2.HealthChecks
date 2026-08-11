# Changelog

All notable changes to this project are documented here.

## 1.0.0

- Removed OS-conditional IBM provider references from the library package.
- Added `netstandard2.0` + `net8.0` + `net10.0` multi-targeting.
- Added `Db2HealthCheckOptions`.
- Added configurable health check timeout and command timeout.
- Added configurable failure status, tags, and result descriptions.
- Added DI-friendly `DbConnection` factory support.
- Added optional `DbProviderFactory` support.
- Added logging hooks.
- Added SourceLink, deterministic build, analyzers, and package metadata.
- Added unit tests for registration, success, failure, and validation paths.
