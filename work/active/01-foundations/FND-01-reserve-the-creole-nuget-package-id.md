# FND-01: Reserve the Creole NuGet package ID

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

The working name Strata was taken on NuGet. Creole is free, and the whitepaper says to reserve it before Phase 0 work begins.

## Scope

- Claim the `Creole` and `Creole.Testing` IDs on nuget.org by publishing them. Reserving the `Creole.*` prefix is deferred by the maintainer's choice.
- Decide the package list for 1.0 (`Creole` with the bundled analyzers and generator, `Creole.Testing`).
- Record which accounts own the packages.

## Acceptance criteria

- [x] nuget.org lists `Creole` and `Creole.Testing` as packages owned by the project's account.

## Blocked by

Nothing. This item can start immediately.

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ Risks](../../../whitepaper.md#risks)

## Progress notes

- 2026-10-18 (Building): The maintainer published `Creole` and `Creole.Testing` 0.1.0-alpha-1 to nuget.org. The original criterion (the `Creole` prefix shown as reserved) was not met: neither gallery page shows the "Prefix Reserved" marker, which the same check finds on `Newtonsoft.Json`. The maintainer chose to defer the prefix reservation and close the card on owning the IDs, so the scope and criterion were reworded. Next: complete.

## Outcome

- `Creole` and `Creole.Testing` 0.1.0-alpha-1 are on nuget.org. Evidence: `https://api.nuget.org/v3-flatcontainer/creole/index.json` and `.../creole.testing/index.json` both list `0.1.0-alpha-1`, and both gallery pages link to the owner profile `IanWold`.
- Package list for 1.0: `Creole`, with the analyzers and generator bundled under `analyzers/dotnet/cs` ([src/Creole/Creole.csproj](../../../src/Creole/Creole.csproj)), and `Creole.Testing`.
- Owner: the nuget.org account `IanWold`.
- Deviation, chosen by the maintainer: the `Creole.*` prefix is not reserved. Others could publish `Creole.Something` packages until it is. To reserve it later, request prefix reservation from the NuGet team.
