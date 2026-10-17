---
name: refactor
description: Carry out a behaviour-preserving refactor across the Creole codebase (renames, moving types, extracting helpers, changing a signature at every call site, applying a pattern repo-wide) safely and in verified batches, with docs, XML docs and whitepaper names kept in step. Use for repetitive, mechanical code changes, with or without a card.
argument-hint: "<the transformation, in words> [card ID]"
---

# Refactor

A refactor changes structure, not behaviour. It is one discrete change, even across hundreds of files, as long as it is one transformation and nothing else.

## Input

`$ARGUMENTS`: the transformation, and optionally the card it serves. Without a card, the commit prefix is `refactor:`.

## Workflow

1. **Scope it.** Find every site: grep for text, but also the semantic sites grep misses: overloads, generic instantiations, `nameof`, XML doc `cref`s, source-generator templates and generated-code expectations, analyzer diagnostic messages, test names, samples, `docs/`, and the whitepaper. Report the count and any sites that need judgment rather than mechanics.
2. **Check it really is behaviour-preserving.** If the change alters public API, it needs a reason: a card, or a decision if the whitepaper names the API. Renaming anything the whitepaper or `docs/` names is still a refactor, but those references change in the same change, and the whitepaper edit counts as editorial only if the meaning is unchanged.
3. **Choose the mechanism**, cheapest reliable first:
   - IDE-grade tooling: Roslyn code fixes or `dotnet format` with an analyzer, when one exists for the transformation.
   - A small script (Python or a C# single-file program) for regular textual patterns. Keep it in the scratchpad unless it will be reused.
   - Manual edits for the sites that need judgment.
   - For very large changes, split the site list into independent batches and run them in parallel subagents with `isolation: "worktree"`. Merge and verify centrally.
4. **Confirm the plan** with the maintainer: mechanism, site count, batches, and anything that is not purely mechanical. Skip confirmation for small, obvious refactors (a single private rename).
5. **Apply in batches**, and after each batch build and run the tests. Where the engines are involved, the differential engine-agreement tests must stay green. A refactor that needs a test's expected value changed is not behaviour-preserving: stop and report it.
6. **Keep everything in step**: XML docs, `docs/`, samples, whitepaper names, and card text that names the old symbol. Run `python3 scripts/work.py check`.
7. **Report**: what changed, the counts, verification results, anything left for judgment, and a commit message (`<ID>: Rename …` or `refactor: …`).

Never mix a behaviour change, a bug fix or a cleanup into the same change. Note them and propose them separately.

## Gotchas

- Generated code: the source generator emits code that tests may compare against. Renames must be reflected in the generator's templates, not only in hand-written code.
- Diagnostic IDs (`CRLnnnn`) are public contract. Never renumber them in a refactor.
