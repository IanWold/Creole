# Diagnostic: a rule's commit semantics differ from its regex reading

Added 2026-10-23.

**What:** Warn when a regular rule matches differently under commit semantics than its regex reading would suggest. The INT-06 prototypes found three idioms that do this: the Parsec-style decimal `Seq(Digits, Optional(Seq('.', Digits)))`, which fails on `1.` followed by anything but a digit; the same decimal with an exponent; and a block comment written without `Atomic`, which fails on `/**/` ([prototype note](../docs/research/2026-10-23-commit-aware-automaton-prototypes.md), finding 4). There are two ways to decide it: exactly, as a product of the commit-aware automaton and the regex DFA; or conservatively, with the note's sufficient condition for Option B (findings 17–18).

**Why it might matter:** These are grammar bugs that the regex reading of a rule hides, and the author reads a rule the regex way. Maximal munch over the term's language disagreed with the interpreter on 3 of 13 realistic tokens and on 10–14% of random cases (finding 4). The usual fix is mechanical (wrap the optional part in `Atomic`), so it could carry a code fix (§ Developer experience for grammar authors).

**Touches:** [§ Static analysis](../../whitepaper.md#static-analysis) (§ Diagnostics), [ANA-04](../active/09-static-analysis/ANA-04-choice-checks.md), [INT-06](../complete/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md), CRL0102.

**First question:** does it catch bugs that CRL0102 (overlapping FIRST in `Or` without `Atomic`) would miss, often enough to justify a new ID? The decimal's failure comes from `Optional` and the comment's from a repetition, not from an `Or` with overlapping alternatives.
