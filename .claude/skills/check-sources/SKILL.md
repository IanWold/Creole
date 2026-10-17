---
name: check-sources
description: Verify the citations and factual claims in a Creole document (the whitepaper, a research note, a decision record, a docs page): that each reference exists with the right authors, venue and year, that its link works, and that the source actually supports what the document says. Read-only report. Use before sharing a document, after a research pass, or when asked whether the references hold up.
argument-hint: "<file path> [section name]"
---

# Check sources

Wrong citations damage a design document's credibility more than missing ones. This skill checks each reference and each claim that rests on one. It reports; it does not edit, unless the maintainer then asks for fixes.

## Input

`$ARGUMENTS`: a Markdown file, optionally narrowed to one section. Default: `whitepaper.md`.

## Workflow

1. **Inventory.** List every reference entry, and every sentence that cites one or states a checkable fact about a paper, library, benchmark or .NET feature (for example "Parlot reports …", "shown to be cubic", "two to six times faster"). Note uncited factual claims too.
2. **Fan out.** Group the claims by source and give each group to a `researcher` subagent, running several in parallel. Each brief asks: does this source exist as cited (authors, title, venue, year, link)? Does it support the claim as worded, including any numbers? Has it been superseded or contradicted?
3. **Spot-check** the researchers on the claims that matter most to the design, by opening the source yourself.
4. **Report** as a table:

   | Claim or reference | Location | Verdict | Evidence | Suggested fix |
   | --- | --- | --- | --- | --- |

   Verdicts: `supported`, `metadata wrong` (with the correct details), `overstated` (the source says less), `unsupported`, `contradicted`, `link broken`, `unverified` (could not access). List uncited factual claims separately.
5. **Next step.** Offer to apply the fixes. Fixes to the whitepaper's References or wording are editorial; if a finding undermines a design argument, propose `/research` or `/decide` instead.

## Gotchas

- Conference papers often have an arXiv preprint with a different title or author order. Cite the published version and say if only the preprint was read.
- Library claims ("Pidgin commits after consumption") should be checked against the library's current source and docs, not memory. Note the version checked.
