# GLL-01: Define the declarative subset

| | |
| --- | --- |
| Epic | [General parsing spike (post-1.0)](../../README.md#gll--general-parsing-spike-post-10) |
| Phase | Research spike — General parsing (post-1.0) |
| Status | Not started |

## Summary

A general backend only works for a declarative subset of the combinators.

## Scope

- Specify the subset: no `Bind`, no layer state, `Not` as a follow restriction, no `Atomic` or `Cut`.
- Analyzer that reports when a grammar leaves the subset.

## Acceptance criteria

- [ ] Subset spec written and the analyzer works on the samples.

## Blocked by

- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)

## Unblocks

- [GLL-02 GLL interpreter prototype](../18-general-parsing-spike/GLL-02-gll-interpreter-prototype.md)

## Whitepaper references

- [§ What has to change](../../../whitepaper.md#what-has-to-change)
- [§ Proposed spike](../../../whitepaper.md#proposed-spike)
