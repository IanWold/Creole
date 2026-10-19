# FND-07: Validate C# 15 features and decide `ParseResult<T>`

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

The design relies on unions and hand-written `[Union]` structs, and may use a `closed` hierarchy for `ParseResult<T>`. Confirm each on the GA compiler and settle the open `ParseResult<T>` question.

## Scope

- Confirm `union` declarations, implicit conversions and switch exhaustiveness.
- Confirm the hand-written `[Union]` struct pattern (`HasValue` plus `TryGetValue`) is exhaustive and does not box.
- Confirm `closed` class hierarchies only as far as the `ParseResult<T>` fallback needs them. Grammar nodes do not use `closed` ([ADR-0001](../../docs/decisions/0001-grammar-node-encoding-and-engine-dispatch.md)). A closed `ParseResult<T>` would need a generic `Failed<T>` case, because a non-generic `Failed` cannot derive from it.
- ParseResult test: does a switch on a union returned from a method need a null arm, and does a hand-written `[Union]` struct with a non-nullable `Value` remove it?
- Choose in order: plain union, then hand-written union, then closed hierarchy. Record the decision in the whitepaper's Still open section.

## Acceptance criteria

- [x] Decision recorded in the whitepaper.
- [x] A spike demonstrates each feature on the pinned .NET 11 compiler, and its results are recorded on this card. The spike is not checked in.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)
- External: .NET 11 / C# 15 GA (expected November 2026)

## Unblocks

- [FND-09 Move to the .NET 11 GA SDK](../01-foundations/FND-09-move-to-the-net-11-ga-sdk.md)
- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)
- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions)
- [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used)
- [§ Still open](../../../whitepaper.md#still-open)

## Artifacts

- [Research: grammar node encoding and dispatch](../../docs/research/2026-10-18-grammar-node-encoding-and-dispatch.md)
- [ADR-0001: Grammar node encoding and engine dispatch](../../docs/decisions/0001-grammar-node-encoding-and-engine-dispatch.md)
- [ADR-0002: `ParseResult<T>` is a plain C# 15 union](../../docs/decisions/0002-parseresult-as-a-plain-union.md)

## Progress notes

- 2026-10-18 (Designing): Started before C# 15 GA by the maintainer's choice: the RC 2 compiler is expected to match GA. The spike is not checked in either, by the maintainer's choice, so criterion 2 was reworded and the results are recorded here. When `global.json` moves to the GA SDK, re-run the spike or the tests that rely on these features. Approach: a scratch spike on SDK 11.0.100-rc.2.26504.105 covering unions, hand-written `[Union]` structs and `closed` for `ParseResult<T>`, then `/decide` on `ParseResult<T>`.
- 2026-10-18 (Designing): Spike run on RC 2 (scratchpad, not checked in). Declared `union ParseResult<T>(Parsed<T>, Recovered<T>, Failed)` with a `Diagnostics` member compiles, and case values convert implicitly, including in generic returns. A switch expression covering the three cases on a union returned from a method needs no null arm and gives no warning; a missing case gives CS8509. Switch statements are never checked. `default(ParseResult<T>)` is unsound: no warning, `SwitchExpressionException` at run time, and `default.Value` dereferences null with no nullable warning. An explicit `null` arm is allowed, matches `default`, and keeps CS8509 for missing cases. A nullable `ParseResult<T>?` needs a null arm (CS8655). A hand-written `[Union]` struct with a non-nullable `object Value` changes nothing: same diagnostics, same exception on `default`. A hand-written generic `[Union]` struct (`Emit<T>`-like, cases `T` and `T[]`) matches through `TryGetValue` with 0 bytes allocated per create and match; a declared union with an `int` case allocates 24 bytes (boxing). Its "no value" state is invisible to exhaustiveness: a switch without a null arm compiles silently and throws for an empty `Emit`. `closed` `Result<T>` with `Ok<T>`, `Fixed<T>`, `Fail<T>` is exhaustive in generic code, shares members on the base, and needs `Fail<T>` to be generic. Next: `/decide` on `ParseResult<T>`; the evidence favours the plain union.
- 2026-10-18 (Designing): ADR-0002 accepted by the maintainer: `ParseResult<T>` is a plain union. Whitepaper § Using C# 15 unions and § Risks and open questions updated; ERR-01 and LAY-03 scopes updated. Next: `/complete FND-07`.

## Outcome

- [ADR-0002](../../docs/decisions/0002-parseresult-as-a-plain-union.md): `ParseResult<T>` is the plain declared union `ParseResult<T>(Parsed<T>, Recovered<T>, Failed)`. The whitepaper's § Using C# 15 unions explains `default` and the optional null arm. The open question moved to Questions raised in review.
- Grammar nodes do not use `closed`: [ADR-0001](../../docs/decisions/0001-grammar-node-encoding-and-engine-dispatch.md), from CORE-01's research.
- Evidence: a scratch spike on SDK 11.0.100-rc.2.26504.105, re-run on 2026-10-18 at completion with the same results as the progress note. It is not checked in, by the maintainer's choice.
- Deviations, both chosen by the maintainer and recorded in the progress notes:
  - Done before C# 15 GA.
  - The spike is not under `tests/`.
- Follow-up: re-run the spike, or the tests that rely on these features, when `global.json` moves to the GA SDK. ERR-01 must keep `default(ParseResult<T>)` out of public APIs. LAY-03 should consider a case type for an empty `Emit<T>`.
