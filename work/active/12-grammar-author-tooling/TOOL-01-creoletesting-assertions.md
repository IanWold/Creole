# TOOL-01: Creole.Testing assertions

| | |
| --- | --- |
| Epic | [Grammar-author tooling](../../README.md#tool--grammar-author-tooling) |
| Phase | Phase 2 — Analysis |
| Size | M |
| Status | Not started |

## Summary

Test helpers that make grammar and error quality checkable.

## Scope

- `Parses`, `FailsAt(line, column, expected)`.
- Snapshot tests of rendered diagnostics.

## Acceptance criteria

- [ ] Example A's test suite uses only Creole.Testing helpers.

## Blocked by

- [FND-04 Test infrastructure](../01-foundations/FND-04-test-infrastructure.md)
- [ERR-03 Diagnostic rendering](../04-errors-and-recovery/ERR-03-diagnostic-rendering.md)
- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)

## Unblocks

- [REL-02 Documentation](../14-release/REL-02-documentation.md)
- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors)
