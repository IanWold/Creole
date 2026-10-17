# GLL-02: GLL interpreter prototype

| | |
| --- | --- |
| Epic | [General parsing spike (post-1.0)](../../README.md#gll--general-parsing-spike-post-10) |
| Phase | Research spike — General parsing (post-1.0) |
| Status | Not started |

## Summary

A GLL interpreter over the subset, following Spiewak's and Meerkat's designs.

## Scope

- Produce a shared packed parse forest.
- Deterministic dispatch for `Or` nodes with disjoint FIRST sets.

## Acceptance criteria

- [ ] Parses an ambiguous expression grammar and returns all derivations.

## Blocked by

- [GLL-01 Define the declarative subset](../18-general-parsing-spike/GLL-01-define-the-declarative-subset.md)

## Unblocks

- [GLL-03 Disambiguation filters](../18-general-parsing-spike/GLL-03-disambiguation-filters.md)

## Whitepaper references

- [§ Proposed spike](../../../whitepaper.md#proposed-spike)
