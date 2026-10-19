# ADR-0009: Rule names come from `Rec`, the enclosing field, and a generated name table

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-19 |
| Cards | [CORE-09](../../active/02-core-combinators/CORE-09-recursion-with-rec.md), [GEN-01](../../active/11-source-generator/GEN-01-generator-skeleton-and-registration.md), [ANA-02](../../active/09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md), [TOOL-02](../../active/12-grammar-author-tooling/TOOL-02-tracing.md), [TOOL-05](../../active/12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md) |
| Whitepaper | [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax), [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors), [§ Source-generated engine](../../../whitepaper.md#source-generated-engine) |
| Research | A spike during CORE-09 planning (scratchpad, not checked in), described under Context. |
| Supersedes | — |

## Context

Run-time tools need rule names, and the node graph does not have them. A grammar's rules are `static readonly` fields, but a node does not know which field holds it. Names are needed by:

| Consumer | Runs | Card |
| --- | --- | --- |
| `GrammarException` rule paths such as `Document → Value → ObjectValue` | Run time: lowering, and Debug enforcement of analysis errors | CORE-09, [ANA-02](../../active/09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md) |
| `ParseOptions.Trace`, which records the rule of every attempt | Run time, on the interpreter | [TOOL-02](../../active/12-grammar-author-tooling/TOOL-02-tracing.md) |
| EBNF and railroad export, whose nonterminals are rule names | Run time | [TOOL-05](../../active/12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md) |
| Analyzer diagnostics such as CRL0002's cycle path | Build time, from syntax | RSLN cards |
| Generated code and `#line` directives | Build time | GEN cards |

Build-time consumers read field names from syntax, so they are unaffected. The run-time consumers have only what the nodes carry.

The whitepaper says `Rec` takes a `[CallerArgumentExpression]` parameter "so diagnostics name the rule `Value`" (§ LINQ query syntax). That works for a reference, `Rec(() => Value)`, whose expression is `() => Value`. It does not work where Example A defines `Value`, `Value = Rec(() => OneOf(...))`, whose expression is the whole lambda. Rules that never pass through `Rec`, such as Example A's `Document`, `ArrayValue` and `ObjectValue`, get no name at all. With names only from `Rec`, Example A's run-time rule path `Document → Value → ObjectValue` and a readable EBNF export (TOOL-05's criterion) cannot be produced.

`Named(label)` (CORE-08) is not a rule name. It sets the label that replaces an expected set in error messages (ERR-02).

**Spike.** On SDK 11.0.100-rc.2.26504.105, a method with a `[CallerMemberName]` parameter called from a static field initializer received the field's name. This held for a direct call (`Value = N(...)` gave `Value`) and for a call nested in another call (`Other = string.Concat("x:", N(...))` gave `Other`). A property initializer gave the property's name. This is measured compiler behaviour. I did not find documentation that states it for field initializers, so it is *unverified* as a language guarantee. FND-09 re-checks RC 2 findings on the GA SDK.

Constraints: names must not add work to parsing (they are read only when lowering, tracing, exporting or throwing), registration must stay Native AOT and trim safe (G8), and the authoring surface should not gain ceremony.

## Options

### A. Names from `Rec`'s reference expression only (the whitepaper's literal position)

`Rec(() => Value)` is named `Value`. Any other `Rec`, and every node outside `Rec`, is unnamed.

- Pros: already designed; no new mechanism.
- Cons: most rules are unnamed. Example A's run-time path would show only `Value`, and its EBNF export would have almost no nonterminal names. This falls short of what § Developer experience promises, and of G10.

### B. A, plus `[CallerMemberName]` as the fallback

`Rec` also takes a `[CallerMemberName]` parameter. A reference expression that is a plain member reference names the rule; otherwise the enclosing member's name does. `Value = Rec(() => OneOf(...))` is then named `Value`.

- Pros: every `Rec` is named, at no cost to the user. Works for grammars built at run time too, since it needs no build step.
- Cons: rules outside `Rec` are still unnamed (`Document`, `ArrayValue`, `ObjectValue`). Relies on undocumented compiler behaviour (spike above). A `Rec` inside a helper method is named after the helper.

### C. B, plus a name table registered by the source generator (recommended)

The generator already registers compiled code for each rule field from the grammar class's static constructor (§ Source-generated engine). It also registers each rule field's name for the node the field holds, in a table keyed by node like `Staging`'s. A registered field name takes precedence over a `Rec` name.

- Pros: every rule field of a `partial` grammar class is named, which covers the whitepaper's examples. No reflection; the table is filled by generated code. Names cost nothing at parse time.
- Cons: grammars in classes that are not `partial`, and grammars built at run time, get names only from `Rec` (B). Run-time tooling therefore depends on the generator for full names, and the name table must land before the tools that need it (TOOL-05 is in Phase 2; GEN-01 is in Phase 3).

### D. Opt-in reflection over a grammar class's static fields

A call such as `Grammar.NameRules(typeof(JsonGrammar))` reads the class's static `Parser<,>` fields and records their names. The parameter is annotated with `[DynamicallyAccessedMembers(PublicFields | NonPublicFields)]`, so it stays trim safe.

- Pros: names every field, `partial` or not, without the generator.
- Cons: ceremony every user must remember; reflection, even if annotated and off the hot path; a second way to do what C does for staged grammars. Could be added later without conflicting with C.

### E. Every combinator takes `[CallerMemberName]`

- Cons: every node built in one initializer gets the same name, so the name does not identify the rule's root node. It adds a hidden parameter to the whole API. Rejected.

## Decision

**C.** Names come from, in order of precedence:

1. **The generated name table.** For each `partial` grammar class, the generator registers every rule field's name for the node that field holds, next to staged-code registration. The table is internal until GEN-01 makes registration public, as with `Staging`.
2. **`Rec`.** `Rec` takes `[CallerArgumentExpression]` and `[CallerMemberName]` parameters. If the reference expression, after removing `static`, `()` and `=>`, is a member reference (identifiers separated by dots), that text is the name: `Rec(() => Value)` is `Value`, `Rec(() => JsonGrammar.Value)` is `JsonGrammar.Value`. Otherwise the enclosing member's name is: `Value = Rec(() => OneOf(...))` is `Value`.
3. **Otherwise the node is unnamed.** Rule paths, traces and exports skip or describe unnamed nodes; they never show internal node type names.

Grammars built at run time and never staged are named only by `Rec`. The whitepaper says so.

The deciding reason: C names every rule in the whitepaper's examples with no extra authoring step and no reflection, by reusing registration the generator already does. B is the part that works without the generator, so it ships first, in CORE-09.

What would change this: if the GA compiler stops passing the field name to `[CallerMemberName]` in field initializers, B falls back to A for definition-site `Rec`s, and C still names the fields. If users of non-staged grammars need full names, add D.

## Consequences

- **CORE-09** builds `Rec` with both caller-info parameters and the name rule above, and rule paths in its lowering errors.
- **The name table** goes in GEN-01 (Phase 3). TOOL-05 needs it for Example A's names, so TOOL-05 is now blocked by GEN-01 and moved to Phase 3. ANA-02 and TOOL-02 are not blocked: they name what `Rec` names until GEN-01 lands.
- **TOOL-05's criterion** ("Example A exports to readable EBNF") depends on the name table, because `ArrayValue` and `ObjectValue` are not `Rec`s.
- **ANA-02** throws rule paths built from these names, and skips unnamed nodes. Before GEN-01, Example A's path shows only `Value`.
- **FND-09** re-checks the `[CallerMemberName]` field-initializer behaviour on the GA SDK, through CORE-09's tests.
- Ruled out: names that cost parse time, and reflection by default.

## Changes made

- Whitepaper: § LINQ query syntax ("Recursion and names") lists the three sources of rule names in order and links this record. § Developer experience for grammar authors says where rule-path names come from. § Source-generated engine's generated static constructor registers each rule's field name, and § Source-generated engine → Discovery says names are registered even for rules that fall back to the interpreter, and that a class that is not `partial` is named only through `Rec`.
- Cards: [CORE-09](../../active/02-core-combinators/CORE-09-recursion-with-rec.md) scope (both caller-info parameters, rule paths in lowering errors). [GEN-01](../../active/11-source-generator/GEN-01-generator-skeleton-and-registration.md) scope and a new criterion for the name table; it now unblocks TOOL-05. [TOOL-05](../../active/12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md) moved to Phase 3 and is blocked by GEN-01. [ANA-02](../../active/09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md) and [TOOL-02](../../active/12-grammar-author-tooling/TOOL-02-tracing.md) scope notes on where names come from. [FND-09](../../active/01-foundations/FND-09-move-to-the-net-11-ga-sdk.md) re-checks the `[CallerMemberName]` finding on GA.
