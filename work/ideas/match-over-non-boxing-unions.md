# `Match` over non-boxing unions without boxing

Added 2026-10-22.

**What:** `MatchPlan` reads its scrutinee through `IUnion.Value`, which boxes the case of a non-boxing union such as `Maybe<T>`. `Match` over a `Maybe<int>` allocates 2,936 bytes per 100 parses, measured during [INT-05](../complete/03-interpreter/INT-05-interpreter-lowering-rewrites.md). The interpreter cannot call C# 15's non-boxing `TryGetValue` pattern without reflection. Options: the source generator calls `TryGetValue` directly; the interpreter uses an adapter registered for each union type; or the cost is accepted and documented.

**Why it might matter:** [ADR-0007](../docs/decisions/0007-maybe-as-a-non-boxing-union.md) assumes nothing in Creole calls `Maybe<T>.Value`, and names a measured hot-path cost from it as a reason to revisit the decision. [§ Design choices](../../whitepaper.md#design-choices) says value-type results flow through plan nodes without boxing. This measurement contradicts both. `Case<T>()` also boxes struct union tokens, but [§ Where unions are not used](../../whitepaper.md#where-unions-are-not-used) already accepts that.

**Touches:** § Design choices, § Where unions are not used, ADR-0007, CORE-05 (`Match`), GEN-02, FND-09.

**First question:** is `Match` over a value-type union common enough on hot paths to be worth a design change, or is a documented exception enough?
