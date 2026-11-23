# Local test coverage

GitHub Actions are intentionally not used for this repository. Measure coverage on the operator's .NET 10 machine.

From the repository root:

```powershell
dotnet test .\DotNetWebSearchApi.sln -c Release --collect:"XPlat Code Coverage"
.\scripts\Show-CoverageSummary.ps1
```

The Coverlet collector writes Cobertura XML beneath `tests/DotNetWebSearchApi.Tests/TestResults`. The summary script selects the newest report, prints aggregate line/branch coverage, and lists the 25 classes with the lowest line coverage (minimum 10 executable lines).

Do not set an arbitrary coverage threshold before obtaining a clean baseline. Use the first report to identify high-risk uncovered logic in request validation, capability filtering, aggregation, provider-health transitions, shared transport and response mapping. Add focused deterministic tests, rerun the full suite and compare the report. Exclude live-provider tests from ordinary coverage runs unless explicitly intended; they are opt-in and depend on upstream availability.

The coverage collector and summary script do not prove tests pass. Record the actual build/test result and the line/branch percentages in `activity/LOG.md` and `activity/STATUS.md`.
