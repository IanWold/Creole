# SearchValues-accelerated self-loops in the commit-aware automaton

Added 2026-10-24.

**What:** At build time, find each automaton state whose transitions on a large set of character classes lead back to itself, and build a `SearchValues<char>` over those classes' characters. When the scan enters such a state, it calls `IndexOfAnyExcept` and skips the whole run at once, instead of stepping one character at a time. If the loop records a match end on every character, as an identifier's loop does in a `Longest`, the scan sets those match ends once, at the end of the run.

**Why it might matter:** INT-06's compiled `Longest` steps once per character, which costs about 2 to 4 ns. INT-05's `ScanPlan` vectorizes runs and measured up to 191× on long runs. INT-06 measured a character-level JSON token `OneOf` compiled as one automaton at 0.59× the speed of dispatch plus scans, because JSON strings are long runs. That result is why only `Longest` compiles today. Acceleration could make other regular subtrees worth compiling, and could speed up the compiled lexers too: identifiers, whitespace, comments and string bodies are all runs. The [DFA research note](../docs/research/2026-10-23-dfa-compilation-under-commit-semantics.md) lists "`SearchValues` for self-loops" among the table options. Other DFA engines are said to accelerate states this way (unverified, from memory: Rust's regex-automata).

**Touches:** [§ Interpreted engine](../../whitepaper.md#interpreted-engine), [INT-06](../complete/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md), [INT-09](../complete/03-interpreter/INT-09-cheaper-automaton-construction.md) (each accelerated state adds a `SearchValues` to lowering), [GEN-05](../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md).

**First question:** with acceleration, does INT-06's trial, the character-level JSON token `OneOf` against dispatch plus scans, turn from 0.59× into a gain? And do `DfaJsonLexer` and `DfaCSharpLexer` gain more than 5%? Short runs are the risk: for one or two characters, a vectorized search costs more than stepping.
