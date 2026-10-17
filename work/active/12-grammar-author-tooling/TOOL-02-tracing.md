# TOOL-02: Tracing

| | |
| --- | --- |
| Epic | [Grammar-author tooling](../../README.md#tool--grammar-author-tooling) |
| Phase | Phase 2 — Analysis |
| Size | M |
| Status | Not started |

## Summary

See what the parser did.

## Scope

- `ParseOptions.Trace` records every attempt: rule, span, outcome and why it failed.
- Render traces as indented text; export a format a viewer can load.
- Tracing runs on the interpreter.

## Acceptance criteria

- [ ] A traced parse of Example C shows mode changes and re-lexing.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)

## Unblocks

- [TOOL-03 Explain](../12-grammar-author-tooling/TOOL-03-explain.md)

## Whitepaper references

- [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors)
