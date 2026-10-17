# ERR-01: Error and result types

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 0 — Core |
| Size | M |
| Status | Not started |

## Summary

The public shapes for results and errors, using C# 15 unions as decided in FND-07.

## Scope

- `ParseError` with span, layer, unexpected, expected and context.
- `Unexpected` and `Expectation` unions with their case records.
- `ParseResult<T>` as `Parsed`, `Recovered` and `Failed`, in the representation chosen by FND-07.
- `Diagnostics` / `DiagnosticBag` ordered by span, with `Count`, `Empty` and `Render()`.

## Acceptance criteria

- [ ] The whitepaper's `switch` on `json.Parse(text)` compiles without a default arm.

## Blocked by

- [FND-07 Validate C# 15 features and decide `ParseResult<T>`](../01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md)
- [SPAN-01 SourceSpan, SourceId and LineMap](../05-source-spans/SPAN-01-sourcespan-sourceid-and-linemap.md)

## Unblocks

- [INT-07 Recursion depth limit](../03-interpreter/INT-07-recursion-depth-limit.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [ERR-03 Diagnostic rendering](../04-errors-and-recovery/ERR-03-diagnostic-rendering.md)
- [ERR-04 Diagnostic withdrawal](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md)
- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)

## Whitepaper references

- [§ The error model](../../../whitepaper.md#the-error-model)
- [§ Results](../../../whitepaper.md#results-1)
- [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions)
