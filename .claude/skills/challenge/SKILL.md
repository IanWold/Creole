---
name: challenge
description: Red-team part of Creole — a whitepaper section, a design idea, a decision record, a research note, a card's plan or a code change — using an independent critic subagent to find flaws, hidden assumptions, missing prior art and unverified claims. Read-only. Use when asked to "keep me honest", poke holes, sanity-check, review or validate something.
argument-hint: "<whitepaper § | file path | card ID | ADR number | 'diff' | a claim in words>"
---

# Challenge

The critique comes from the `critic` subagent, which has a fresh context. That matters: a reviewer who helped write something, or who has just heard the maintainer argue for it, is biased toward it. Your job in the main conversation is to brief the critic well and then weigh what it finds. Do not edit files.

## Input

`$ARGUMENTS`: what to challenge. A whitepaper section, a path, a card ID (its plan and work so far), an ADR number, `diff` (uncommitted changes, once git exists), or a claim stated in words, including "what we just discussed".

## Workflow

1. **Write a self-contained brief.** The critic cannot see this conversation, so include:
   - the target: file paths and sections, or the claim or design restated in full if it exists only in conversation;
   - the claims, guarantees and assumptions to test (complexity bounds, allocation or linear-time promises, semantics, claims about other libraries);
   - the relevant whitepaper sections, decisions and cards to check consistency against;
   - the angles to cover: internal consistency, soundness (adversarial grammars and inputs), evidence behind factual claims, prior art, .NET reality (the C# 15 preview, Native AOT and trimming, source-generator constraints), and scope against the goals and non-goals.

   Do not include your own opinion of the target, so you do not anchor the critic.
2. **Run the critic.** For a large target, split it into independent parts and run critics in parallel.
3. **Weigh the findings.** For each one, say whether you agree and why. Check a finding yourself when it is cheap to do so. Drop findings that are wrong, and say so.
4. **Report**: blocking, significant and minor findings with your assessment of each, what held up, and next steps: `/research` for open factual questions, `/decide` for design forks, `/idea` or `/new-card` for new work. Offer these; do not do them.

## Gotchas

- "Challenge this" right after a long design discussion: put the design in the brief as concrete statements, not "the approach we discussed".
