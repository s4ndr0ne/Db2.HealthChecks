# Public API Compatibility

The public API is intentionally limited to `HealthChecksExtensions.AddDb2Check` and
`Db2HealthCheckOptions`. Public members are documented and must not be removed or have their
signatures changed in a minor or patch release.

The public API surface is tracked with `Microsoft.CodeAnalysis.PublicApiAnalyzers`:

- `src/Db2.HealthChecks/PublicAPI.Shipped.txt` contains the shipped `1.0.0` surface.
- `src/Db2.HealthChecks/PublicAPI.Unshipped.txt` contains not-yet-released additions.

CI fails when the baseline is stale. To regenerate the baseline, run:

```bash
dotnet format src/Db2.HealthChecks/Db2.HealthChecks.csproj --diagnostics RS0016 --no-restore
```

then move the generated entries from `PublicAPI.Unshipped.txt` to `PublicAPI.Shipped.txt` before a
release. `RS0026` is suppressed because the two `AddDb2Check` overloads with optional parameters are
the intentionally designed public surface and there is no earlier shipped API.
