# ADR-0002: `ParseResult<T>` is a plain C# 15 union

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-18 |
| Cards | [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md), [ERR-01](../../active/04-errors-and-recovery/ERR-01-error-and-result-types.md), [LAY-03](../../active/06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md) |
| Whitepaper | [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Still open](../../../whitepaper.md#still-open) |
| Research | FND-07's spike, recorded in its progress notes (2026-10-18). [Unions spec](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md), § Nullability and § Union exhaustiveness. |
| Supersedes | — |

## Context

§ Still open asked whether `ParseResult<T>` should be a union or a closed hierarchy. A union's `Value` is null in a `default` instance, so switches might need a null arm. The whitepaper set an order of preference:

1. Keep the plain union if it works.
2. Otherwise use a hand-written `[Union]` struct.
3. Fall back to a closed hierarchy only if neither works.

FND-07's spike ran on the .NET 11 RC 2 compiler. The maintainer chose to decide before GA, and to re-run the spike when `global.json` moves to the GA SDK.

## Options

### A. Plain union declaration (the whitepaper's position)

`public union ParseResult<T>(Parsed<T>, Recovered<T>, Failed)`.

- Pros:
  - A switch expression covering the three cases needs no null arm and gives no warning, even on a union returned from a method. A missing case is CS8509.
  - Members such as `Diagnostics` can be declared in the union body.
  - Case values convert implicitly, including in generic returns.
- Cons: `default(ParseResult<T>)` is unsound.
  - The compiler gives no warning.
  - A switch throws `SwitchExpressionException`, and `.Value` throws `NullReferenceException`.
  - The spec chose this. Its nullability rule treats `Value` as not-null when no case type is nullable. Its "default" well-formedness rules are marked removed.
  - An explicit `null` arm is allowed, matches `default`, and keeps CS8509 for missing cases.
- Performance: the cases are classes, so the union adds no boxing. It is a struct holding one reference.

### B. Hand-written `[Union]` struct with a non-nullable `Value`

- Measured: no difference from A. The diagnostics are the same, and so is the exception on `default`. It adds code and gains nothing.

### C. `closed` class hierarchy

- Pros: no `default` problem, because a reference-type result is never a `default` struct. Cases share members on the base.
- Cons:
  - Every case must be generic (`Failed<T>`), because of the C# 15 type-parameter rule.
  - Callers lose the union's implicit conversions.
  - Callers must still handle `null` when the result is nullable (CS8655).
  - It allocates nothing less: case objects are allocated either way.

### D. Defer until GA

- Cons: it blocks ERR-01 for about a month. The spec has no open questions that touch these rules.

## Decision

Option A: `ParseResult<T>` is the declared union `ParseResult<T>(Parsed<T>, Recovered<T>, Failed)`. Accepted by the maintainer on 2026-10-18.

- **No `default` values from Creole.** No Creole API returns, stores or exposes `default(ParseResult<T>)`. Every `Parse` overload returns one of the three cases.
- **Callers.** Callers switch over the three cases with no null arm. Code that might hold a `default`, such as an uninitialized array element, may add a `null` arm, which keeps the missing-case check.

Deciding reason: the plain union passes the whitepaper's own test. Switches on returned results need no null arm, and the alternatives fix nothing that matters.

What would change this: the GA compiler behaving differently on the re-run, or evidence that users meet `default` results in practice.

## Consequences

- **Easier:** the whitepaper's sample switch on `json.Parse(text)` compiles as written. ERR-01 can start once SPAN-01 is done.
- **Harder:** a `default(ParseResult<T>)` fails at run time, not at compile time. ERR-01's tests should cover that no public API returns one.
- **Related finding (not decided here).** A hand-written `[Union]` struct whose empty state is a null `Value` hides that state from exhaustiveness checking. `Emit<T>` is designed that way: a switch without a `null` arm compiles and then throws for an empty `Emit`. LAY-03 should consider making "no output" a case type.
- **Watch:** re-run FND-07's spike on the GA SDK.

## Changes made

- **Whitepaper:**
  - § Using C# 15 unions: a paragraph on `default` and the null arm, linking this record.
  - § Risks and open questions: the `ParseResult<T>` item moved from Still open to Questions raised in review.
- **Cards:**
  - [ERR-01](../../active/04-errors-and-recovery/ERR-01-error-and-result-types.md): scope names the union and the no-`default` rule.
  - [LAY-03](../../active/06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md): scope notes the `Emit<T>` empty-state finding.
  - [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md): decision recorded; completed.
