---
name: research
description: Act as Creole's research partner — investigate a question using papers, other libraries' source and docs, .NET documentation and measurements, then write a cited research note with implications for the design. Use for prior-art surveys, literature checks, "has anyone done X", and validating claims in the whitepaper.
argument-hint: "<question, topic or card ID> [--quick]"
---

# Research

Find out what is actually known, with sources, and say what it means for Creole. Rules: CONTRIBUTING.md § Research notes, and CLAUDE.md § Honesty. These matter more here than anywhere else.

## Input

`$ARGUMENTS`: a question, a topic, or a card ID (research the questions that card raises). `--quick` answers in chat without writing a note; offer to write one if the findings matter.

## Workflow

1. **Frame the question.** Restate it as one or more answerable questions. Read the whitepaper sections it touches and the existing research and decision indexes, so you do not repeat past work. Tell the maintainer the framing in two or three lines. If the question is vague, ask before searching.
2. **Plan sources.** Typical sources for Creole:
   - Papers: PLDI, ICFP, OOPSLA, SLE, PEPM, the Haskell Symposium; arXiv; authors' pages. Start from the whitepaper's References and follow citations both ways.
   - Libraries: Sprache, Superpower, Pidgin, Parlot, Yoakke, FParsec, ANTLR, chumsky, nom, winnow, FastParse, parsley, megaparsec, tree-sitter, Lezer, Roslyn's own lexer and parser. Read their source and issues, not only READMEs.
   - .NET: learn.microsoft.com, dotnet/csharplang proposals and meeting notes, dotnet/runtime and dotnet/roslyn issues, for anything about C# 15, unions, source generators, AOT, Span and pipelines.
3. **Search and read.** Split the work into independent sub-questions and give each to a `researcher` subagent, running them in parallel; their search noise stays out of this conversation. Handle a single narrow question directly with WebSearch and WebFetch. Read primary sources; a blog post summarizing a paper is a pointer to the paper, not a source. Before relying on a researcher's key citations, spot-check them yourself.
4. **Look for disconfirming evidence on purpose.** Search for prior art that already does what Creole plans, results that failed to replicate, and known limits of the techniques the whitepaper relies on. Report what you searched even if nothing turned up.
5. **Measure when a measurement settles it.** If a small experiment (a single-file C# program, a benchmark sketch) would answer the question better than reading, propose it. Run it only with the maintainer's agreement, and keep it in the scratchpad unless asked to keep it.
6. **Write the note** (unless `--quick`): `work/docs/research/YYYY-MM-DD-<slug>.md` from the template in [work/docs/research/README.md](../../../work/docs/research/README.md). Mark each source with how much of it you read; mark memory-only claims *unverified*. Set Confidence honestly.
7. **Wire it in.** Add the note to the research index. Link it from the `## Artifacts` of every card it serves. Append a progress note to those cards. Do not edit the whitepaper: research informs decisions, and decisions change the whitepaper.
8. **Report** in chat: the short answer, the most important disconfirming finding, the implications, and the next step (usually `/decide`, a new card, or `/idea`). Run `python3 scripts/work.py check`. Suggest a commit message (`research: <question>` or `<ID>: Research <topic>`).

## Gotchas

- Library behaviour changes between versions. When a finding is about Pidgin, Parlot or another library, record the version or commit checked.
- Search engines favour recent blog posts over the papers that matter. For academic questions, start from the whitepaper's References and follow citations forward (who cites this paper) as well as backward.
