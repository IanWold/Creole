# Creole: A Layered, Analyzable Parser-Combinator Library for .NET

Creole is a proposed parser-combinator library for .NET in which lexers, token transformers and parsers are all written with one combinator vocabulary, over any input type, and chained into streaming pipelines.

Three ideas set it apart from existing C# parser-combinator libraries:

1. **Layered pipelines with feedback.** Each layer's output stream is the next layer's input. A downstream layer can backtrack without re-running upstream work, and can switch an upstream layer's lexing mode under defined rollback rules.
2. **Parsers as data.** Grammars are inspectable trees, not delegates. That allows construction-time and build-time checks for left recursion, infinite loops and shadowed alternatives, plus an opt-in deterministic mode with a linear-time guarantee.
3. **Staged compilation.** A Roslyn source generator compiles grammars to plain C# at build time, safe for Native AOT. A later phase fuses a lexer and the parser above it into one loop that never stores tokens, following flap (PLDI 2023).

Source spans travel with every item through every layer. An error found by the final parser therefore points at characters in the original text, and so does every diagnostic from recovery.

Diagnostics are a first-class product for two audiences. People using a Creole-built parser get precise, recoverable errors rendered against their source. People writing grammars get build-time analysis with code fixes, traceable execution and test helpers (§ Developer experience). Creole is aimed at full programming-language grammars as much as at markup and query languages, and is built for .NET 11 and C# 15.

## Background and motivation

The .NET ecosystem has four mature combinator libraries, each a step past the last, and none treats multi-stage parsing or grammar analysis as a first-class concern.

| Library | Model | What it added |
| --- | --- | --- |
| [Sprache](https://github.com/sprache/Sprache) | Parsers are delegates over character input. `Or` backtracks; `XOr` commits. | Idiomatic combinators and LINQ query syntax in C#. |
| [Superpower](https://github.com/datalust/superpower) | Two fixed stages: a `Tokenizer<TKind>` produces a token list, then `TokenListParser<TKind, T>` parses it. | Separate lexing, friendlier errors, much less backtracking. |
| [Pidgin](https://github.com/benjamin-hodgson/Pidgin) | `Parser<TToken, T>` over any token type. `Or` commits after consumption; `Try` backtracks. | Generic input, high throughput, low allocation. |
| [Parlot](https://github.com/sebastienros/parlot) | Parser classes over text, with whitespace and comments handled by `Terms`. Its generator, so far only in preview packages, runs `[GenerateParser]` methods at build time and emits the body of a `partial` parse method the user declares. | The fastest .NET combinators by its own benchmarks, build-time generation, and recursion limits for untrusted input. |

Other .NET tools sit beside these rather than competing directly. ANTLR's C# target is a parser generator; Irony builds LALR parsers from grammars written in C#; FParsec brings Parsec to F#; [Yoakke](https://github.com/LanguageDev/Yoakke) generates lexers and parsers from attributes but is still early, with nightly packages only. Parlot matters most: it already shows that build-time generation works for .NET combinators, so Creole's case cannot rest on speed alone.

Pidgin can already run in stages: a `Parser<char, Token>` produces a list, and a `Parser<Token, Ast>` consumes it. But the stages share nothing. Positions, error reporting, buffering and backtracking are each stage's own problem, and nothing lets a parser influence its lexer. Sprache, Superpower and Pidgin also represent parsers as opaque code, so they cannot find a grammar bug before it runs or compile a grammar ahead of time. Parlot compiles ahead of time by running grammar methods inside its generator, but none of the four analyzes a grammar for bugs before it runs (§ Comparison).

Meanwhile, research since Spiewak's GLL combinators (2010) has moved in four directions:

- **General parsing.** Parsing with Derivatives (Might, Darais, Spiewak, ICFP 2011) was later shown to be cubic by Adams, Hollenbeck and Might (PLDI 2016) and made cheaper by Parsing with Zippers (Darragh and Adams, ICFP 2020). Meerkat (Izmaylova, Afroozeh, van der Storm, PEPM 2016) delivered practical general combinators in Scala.
- **Analyzable combinators.** Selective applicative functors (Mokhov et al., ICFP 2019) sit between applicative and monadic interfaces: every branch is declared statically, so the grammar stays inspectable, but the choice between branches is made at run time.
- **Typed, staged combinators.** Krishnaswami and Yallop (PLDI 2019) gave a type system that identifies grammars parseable by LL(1)-style recursive descent, then staged them into code with a linear-time guarantee. Parsley (Willis, Wu, Pickering, ICFP 2020) represents the grammar as a tree and partially evaluates it with Typed Template Haskell.
- **Fusion.** flap (Yallop, Xie, Krishnaswami, PLDI 2023) fuses separately defined lexers and parsers into token-free code. The authors report it running two to six times faster than ocamlyacc on their benchmarks.

Industry libraries filled in practical gaps: chumsky (Rust) made error recovery a first-class strategy, and FastParse (Scala) popularized explicit cuts.

**Who Creole is for.** Today's .NET combinator libraries are used mostly for markup, configuration and query languages. Full programming languages need more: operator precedence, statements and blocks, indentation or context-sensitive lexing, many diagnostics per file, and editor tooling. Creole treats those grammars as first-class targets without making a ten-line query grammar any harder to write.

Creole's thesis is that these results have landed in Haskell, OCaml, Scala and Rust, and C# now has the pieces to adopt them. Roslyn source generators provide build-time staging; `SearchValues<char>` provides vectorized character-class scanning; ref structs and spans provide allocation-free cursors; and C# 15 unions make results, errors and syntax trees exhaustively checkable.

## Design goals and non-goals

Creole commits to twelve goals. G1 to G11 are for 1.0; G12, incremental re-parsing, follows in 1.x. General (ambiguous) parsing is not one of the goals: it is explored separately rather than promised.

| ID | Goal | Measured by |
| --- | --- | --- |
| G1 | One combinator vocabulary at every layer, over `char`, tokens or any `T` | The same `Many`, `SepBy`, `Or` work in lexer and parser |
| G2 | Streaming n-layer pipelines: lazy, pull-based, bounded memory | Peak memory tracks the largest backtracking window, not input size |
| G3 | Cross-layer backtracking without re-running upstream work | Each upstream item is produced once (mode changes excepted) |
| G4 | Controlled feedback: a parser can change the mode of any layer upstream of it | Defined invalidation and rollback rules across layers (§ Feedback) |
| G5 | Source spans on every item at every layer, in bytes and characters | Any diagnostic renders against the original text |
| G6 | Analyzable grammars | Left recursion, nullable loops and shadowed alternatives caught before parsing |
| G7 | Two engines, one semantics | Interpreted and generated engines agree on values, positions and errors |
| G8 | Built for .NET 11 and C# 15; Native AOT and trimming safe | Unions in public shapes; no runtime IL emission or reflection on the hot path |
| G9 | Excellent diagnostics for the people using a parser | Many precise, recoverable errors per parse, without cascades (§ Errors) |
| G10 | Excellent developer experience for grammar authors | Analyzers with fixes, traceable execution, test helpers (§ Developer experience) |
| G11 | Full programming-language grammars as a first-class target | Indentation, context-sensitive lexing and precedence need no workarounds; small grammars stay small |
| G12 | Incremental re-parsing for editors (1.x) | Re-parse cost tracks the size of the edit, not the file (§ Incremental re-parsing) |

**Non-goals for 1.0**

- General CFG parsing with parse forests in 1.0. It is under exploration (§ General parsing backend) and would plug into the same grammar tree.
- Binary-format tuning. `Parser<byte, T>` works, but nothing is optimized for it.
- A Sprache compatibility adapter. Migration is by guide instead (§ Comparison).

## Core model: parsers as data

Every combinator call builds an immutable node in a grammar tree, and nothing runs until an engine executes that tree. This is the deep embedding Parsley uses, in contrast to delegates (Sprache, Superpower) or objects with a virtual parse method (Pidgin).

Representing grammars as data buys four things:

1. **Analysis.** Nullability, FIRST sets and left recursion can be computed before any input is seen.
2. **Staging.** A source generator can compile the tree to straight-line C#.
3. **Rewriting.** Engines can merge adjacent maps, turn choices into jump tables and compile regular subtrees to automata.
4. **Documentation.** A grammar can be printed as EBNF or a railroad diagram.

### Types

```csharp
// A grammar node. Immutable; compared by identity. Engines interpret or compile it.
// Constructors are internal and node classes are internal sealed, so other assemblies
// cannot add nodes.
public abstract class Parser<TIn, TOut>
{
    internal Parser() { }

    internal abstract NodeKind Kind { get; }
}

public readonly record struct Unit;

// Tokens with a discrete kind make FIRST sets precise and enable jump-table dispatch.
public interface IKinded<TKind> where TKind : struct, Enum
{
    TKind Kind { get; }
}
```

Nodes fall into seven families:

| Family | Nodes | Analyzable? |
| --- | --- | --- |
| Primitives | `Token(x)`, `Kind(k)`, char classes, `Any`, `End`, `Satisfy(pred)` | Yes, except `Satisfy`, which is treated as matching anything |
| Sequence and map | `Seq(...)`, `Select`, `Before`, `Then`, `Between` | Yes |
| Choice | `Or`, `OneOf`, `Longest`, `Switch` | Yes |
| Repetition | `Many`, `AtLeastOnce`, `Repeat`, `SepBy`, `SkipMany` | Yes |
| Control | `Atomic`, `Lookahead`, `Not`, `Cut`, `Recover`, `Named` | Yes |
| Recursion | `Rec(() => p)` | Yes, by fixpoint |
| Dependent | `Bind`, `SelectMany` | No: the continuation is code, so analysis assumes the worst |

Opaque nodes still run correctly in every engine. They only weaken what analysis can prove, and the analyzer says so (CRL0201).

**How engines reach nodes.** Many nodes hide a type: `Select` is a `Map<TIn, TA, TOut>` seen as a `Parser<TIn, TOut>`, and C# has no pattern that recovers `TA`. So every engine operation that needs a node's types is an `internal abstract` method on `Parser<TIn, TOut>`. Each node overrides it and passes its typed fields to the engine, as in `Lower(ctx) => ctx.Map(source, selector)`. A node that does not implement every operation does not compile. Consumers that need only structure (analysis, EBNF export, Explain, fuzzing) read `Kind` and the node's children and switch on `NodeKind` with switch expressions, so a missing kind is a build error. The choice, and why a `closed` hierarchy cannot express these nodes, is in [ADR-0001](work/docs/decisions/0001-grammar-node-encoding-and-engine-dispatch.md).

### Authoring ergonomics

C# cannot infer `TIn` from a call such as `Char('a')`. Grammars therefore inherit a base class that fixes the input type, and its static members are then in scope unqualified:

```csharp
public sealed partial class Identifiers : CharGrammar          // CharGrammar : Grammar<char>
{
    public static readonly Parser<char, string> Identifier =
        Seq(Letter.Or(Char('_')), LetterOrDigit.Or(Char('_')).SkipMany())
            .Text()
            .Named("identifier");
}
```

- `Grammar<TIn>` provides `Any`, `Token`, `Satisfy`, `End`, `Seq`, `OneOf`, `Longest`, `Rec`, `Return` and `Fail`.
- `TokenGrammar<TToken, TKind>` adds `Kind(k)` for tokens implementing `IKinded<TKind>`.
- `CharGrammar` adds character classes (`Char`, `Range`, `Digit`, `Letter`, `Whitespace`, `String`, `CIString`) and `Text()` / `Slice()` for capturing source text.

**Two sequencing styles, one node.** `Seq(p, q, r).Select((a, b, c) => ...)` and LINQ query syntax both build the same analyzable sequence node. LINQ query syntax is fully supported, even though C# compiles it to `SelectMany`, which is a dependent bind. Creole recovers the structure, as described next, so independent clauses become `Seq` nodes and only genuinely dependent clauses are opaque.

### LINQ query syntax

Query syntax stays first-class: it reads well and Sprache users know it. Each `from` clause is classified separately as a sequence step or a bind:

```csharp
// Independent clauses: analyzed exactly like Seq(Kind(J.String), Kind(J.Colon), Value).
static readonly Parser<JTok, KeyValuePair<string, JsonValue>> Member =
    from key   in Kind(J.String)
    from colon in Kind(J.Colon)
    from value in Value
    select KeyValuePair.Create(Unescape(key.Text), value);

// A dependent clause: the count is data, so this step is a genuine bind.
static readonly Parser<byte, byte[]> Blob =
    from length in UInt16
    from bytes  in Any.Repeat(length)
    select bytes.ToArray();
```

- **Generated engine.** The generator reads the query's syntax and checks whether a clause mentions an earlier range variable.
- **Interpreter.** Creole's `SelectMany` takes each clause as an `Expression<Func<A, Parser<TIn, B>>>`. At construction time Creole checks whether the parameter appears in the expression tree. If not, it evaluates the expression once and builds a `Seq` node; if so, it compiles the expression once and builds a `Bind` node. Parsing never touches expression trees, and under Native AOT the one-time evaluation is interpreted.

The costs, stated plainly:

- A `from` clause must be an expression lambda that is valid in an expression tree. Clauses are nearly always a parser reference or call, so this rarely bites.
- Queries with three or more clauses make C# allocate a transparent-identifier object per extra clause per match. The generator removes these; interpreted hot paths may prefer `Seq`.
- `where` becomes a filter that fails committed if it rejects after consuming input, and `let` becomes a map.

**Recursion and names.** `Rec(() => Value)` defers a reference so fields can refer to each other. `Rec` takes a `[CallerArgumentExpression]` parameter, so diagnostics name the rule `Value` without extra ceremony. `Named("value")` sets the label used in error messages.

## Using C# 15 unions

Creole uses C# 15 unions wherever a value is one of a fixed set of shapes, so the compiler checks that every consumer handles every case. Unions stay out of the innermost parse loop, because the `union` keyword stores its case in an `object?` and boxes value types.

| Where | Union | What it buys |
| --- | --- | --- |
| Parse results | `ParseResult<T>(Parsed<T>, Recovered<T>, Failed)` | Callers cannot forget the parsed-with-errors case |
| Error details | `Unexpected(UnexpectedItem, EndOfInput, CustomMessage)` and `Expectation(Label, ExpectedItem, EndOfInput)` | Renderers and language servers switch exhaustively |
| Lexer modes | e.g. `LexMode(Code, TemplateText, Regex)` | Layer.Modal switches cover every case; cases should carry no data (§ Feedback) |
| Selective choice | `p.Match(Case(pa, (A a, TA x) => …), Case(pb, (B b, TB y) => …))` over a union-valued `p` | Analysis checks every case has a branch (CRL0105) |
| User syntax trees | e.g. `JsonValue(JsonString, JsonNumber, ...)` | Implicit conversions replace casts in grammar actions |

```csharp
public sealed record Parsed<T>(T Value);
public sealed record Recovered<T>(T Value, Diagnostics Diagnostics);
public sealed record Failed(Diagnostics Diagnostics);

public union ParseResult<T>(Parsed<T>, Recovered<T>, Failed)
{
    public Diagnostics Diagnostics => this switch
    {
        Parsed<T>      => Diagnostics.Empty,
        Recovered<T> r => r.Diagnostics,
        Failed f       => f.Diagnostics,
    };
}

string summary = json.Parse(text) switch
{
    Parsed<JsonValue>         => "ok",
    Recovered<JsonValue> r    => $"parsed with {r.Diagnostics.Count} errors",
    Failed f                  => f.Diagnostics.Render(),
};
```

The switch needs no null arm: the compiler treats a union's `Value` as non-null when no case type is nullable. Only `default(ParseResult<T>)` holds null, and it fails at run time with `SwitchExpressionException`. No Creole API returns one. Code that might hold a `default`, such as an uninitialized array element, can add a `null` arm without losing the missing-case warning ([ADR-0002](work/docs/decisions/0002-parseresult-as-a-plain-union.md)).

A selective `Match` over a union keeps every branch visible to analysis:

```csharp
public union Header(V1Header, V2Header);

static readonly Parser<byte, Record> Record =
    HeaderParser.Match(
        Case(BodyV1, static (V1Header h, Body body) => new Record(h.Id, body)),
        Case(BodyV2, static (V2Header h, Body body) => new Record(h.Id, body)));
// Dropping a case is analyzer error CRL0105, not a runtime surprise.
```

Each case is a branch parser and a function that combines the union's case with the branch's result. The lambda's parameter types name the case, so every type is inferred. C# cannot infer the rest from one explicit type argument, which is why the case is not written `Case<V1Header>(...)` ([ADR-0005](work/docs/decisions/0005-match-cases-infer-every-type-from-a-typed-lambda.md), restated in [ADR-0006](work/docs/decisions/0006-switch-and-match-take-a-collection-beyond-eight-cases.md)). `Match` accepts any type implementing `IUnion`, which declared unions do. Cases are tried in order; if none applies, `Match` fails.

### Where unions are not used

- **Hot-path values.** Steps return `bool` and read consumption from the cursor (§ Combinator semantics), so no union is involved. `Emit<T>` is a hand-written `[Union]` struct using C# 15's non-boxing access pattern (`HasValue` plus `TryGetValue`), so it keeps exhaustive matching without allocating.
- **Grammar nodes.** Neither unions nor C# 15's `closed` classes can hold nodes such as `Map<TIn, TA, TOut>`, whose type `TA` does not appear in `Parser<TIn, TOut>`. Instead each engine operation is an abstract method every node must implement (§ Types, [ADR-0001](work/docs/decisions/0001-grammar-node-encoding-and-engine-dispatch.md)). Adding a combinator without teaching the interpreter is a build error, which protects G7. The source generator reads syntax rather than node types, so its share of G7 rests on the CRL0301 fallback and the differential tests (§ Equivalence). User-written scanners join through a single `Custom` node (§ Custom primitives), so the set of nodes stays fixed.
- **Tokens, by default.** Tokens can be unions, matched with `Case<Ident>()` instead of `Kind(k)`, and analysis treats each case type as a kind. But a union token boxes struct cases, so a lexer would allocate per token. `IKinded<TKind>` struct tokens remain the fast path, and fused layers never materialize tokens either way.

### Small API consequences

`Select` and `As` are instance methods on `Parser<TIn, TOut>`, so a grammar action can name only its result type and let the union's implicit conversion do the rest: `Kind(J.String).Select<JsonValue>(t => new JsonString(...))`.

## Combinator semantics

Creole uses Parsec-style commit semantics: a choice tries its next alternative only if the previous one failed without consuming input, and backtracking is opt-in through `Atomic`. This keeps lookahead bounded, error messages precise and stream buffers small.

### Results

Every parse step ends in one of four outcomes:

| Outcome | Meaning | Choice tries next alternative? |
| --- | --- | --- |
| Success, empty | Succeeded without consuming | n/a |
| Success, consumed | Succeeded and advanced | n/a |
| Failure, empty | Failed at the starting position | Yes |
| Failure, committed | Failed after consuming input | No |

A fifth outcome, **Starved**, exists only in streaming mode and is never visible to combinators (see § Streaming).

**Representation.** These outcomes are a model, not a type. On the hot path a step returns `bool`, and consumption is read from the cursor: a step consumed input if the cursor moved. Starved is a sticky flag on the parse state. `Or`, `Atomic`, `Not`, `Lookahead` and `Recover` check it before acting on a failure, so a starved attempt is never mistaken for an ordinary empty failure.

### Choice and backtracking

- `Or` / `OneOf`: ordered choice with commit semantics.
- `Atomic(p)`: if `p` fails, rewind to the start so the failure counts as empty. Equivalent to Parsec's `try`.
- `Lookahead(p)`: run `p`, then rewind on success. `Not(p)`: succeed without consuming if `p` fails.
- `Cut`: from this point, failure stays committed even inside an enclosing `Atomic`. After `if` and `(` are seen, a failure is reported here instead of being retried elsewhere, which improves messages and lets buffers release earlier.
- Literal primitives (`String("<=")`, `Kind(k)`) are atomic by construction: they fail without consuming.

### Longest match

`Longest(p1, ..., pn)` runs every alternative atomically from the same position and keeps the one that consumed the most input; ties go to the earliest. This is the maximal-munch rule lexers need (`<=` beats `<`, `iffy` beats `if`), expressed as an ordinary combinator that also works at token level. When all alternatives are regular, engines compile it to a single DFA instead of n attempts.

### Selective choice

`Switch` is the selective-functor form. The scrutinee is parsed first, and its value picks one of a statically known set of branches:

```csharp
Parser<byte, Record> record =
    Version.Switch(
        (1, RecordV1),
        (2, RecordV2),
        @default: Fail<Record>("unsupported version"));
```

Every branch is visible to analysis, unlike `Bind`. `Match` is the same idea over a union's cases (§ Using C# 15 unions).

Both take up to eight cases as separate arguments, so a null branch is reported by its expression. Beyond that, they take the cases as a collection, such as `Opcode.Switch([(0x01, Nop), /* … */ (0x2A, Halt)], @default: …)` or a table built in code. Nesting is no substitute: a nested `Switch` would parse the scrutinee again ([ADR-0006](work/docs/decisions/0006-switch-and-match-take-a-collection-beyond-eight-cases.md)). `Bind` remains for grammars that are genuinely data-dependent, such as a length prefix followed by that many items.

### Repetition and structure

`Many`, `AtLeastOnce`, `Repeat(min, max)`, `SepBy`, `SepBy1`, `SepEndBy`, `Until(terminator)` and allocation-free `SkipMany` / `SkipAtLeastOnce` behave as in Pidgin. `Between(open, close)`, `Before(q)` and `Then(q)` cover the usual sequencing shapes.

### Expressions

`Expression.Build(atom, table)` builds one node from an atom and an operator table. Grammars never need left recursion for operators, and analysis reads the table directly. The table is the precedence-table pattern of Willis and Wu (Haskell Symposium 2021). Creole parses it by precedence climbing rather than folding it into chains, so a prefix operator can appear in any operand position ([ADR-0004](work/docs/decisions/0004-expression-build-operator-table-and-semantics.md)).

```csharp
static readonly Parser<char, double> Expr = Expression.Build(Atom, op =>
[
    [op.Postfix(Char('!'), (x, _) => Factorial(x))],
    [op.InfixR(Char('^'), (l, _, r) => Math.Pow(l, r))],
    [op.Prefix(Char('-'), (_, x) => -x)],
    [op.InfixL(Char('*'), (l, _, r) => l * r), op.InfixL(Char('/'), (l, _, r) => l / r)],
    [op.InfixL(Char('+'), (l, _, r) => l + r), op.InfixL(Char('-'), (l, _, r) => l - r)],
    [op.InfixN(Char('<'), (l, _, r) => l < r ? 1 : 0)],
]);
```

- **Table.** Levels are listed tightest first, and every level produces the atom's type. Each operator is a parser of any type plus a combine function that receives the operator's value, so an AST node can record the operator's span. The lambda's `op` fixes the result type, so the combine functions need no annotations.
- **Checks.** `Build` throws a `GrammarException` for an empty table or level, or a level that mixes infix associativities. Prefix and postfix operators may share a level with infix ones.
- **Operands.** Wherever an operand is expected, prefix operators are tried first, and a prefix operator's operand is parsed at its own level. So `2 ^ -x` is `2 ^ (-x)` and `-x ^ 2` is `-(x ^ 2)`. Prefix and postfix operators repeat: `- -x`, `f()()`. At one level, prefix applies before postfix.
- **Operators.** After an operand, postfix and infix operators are tried, tighter levels first. Prefix operators are never tried there, so `-` can be both prefix and infix. A token used as both postfix and infix goes to whichever is tried first, which analysis reports as an ambiguity.
- **Non-associative chains.** In `a < b < c`, the second `<` fails committed with "'<' cannot be chained; add parentheses". `a < b + 1` is unaffected.
- **Errors.** Ordinary commit semantics apply. An operator that fails without consuming ends the expression; one that consumed input commits, so a missing right operand is an error at that point. Operand recursion counts against `ParseOptions.MaxDepth`.

This differs from Pidgin's `ExpressionParser` in three ways: Pidgin applies prefix and postfix operators once unless they are marked chainable, it cannot parse `2 ^ -x` when `-` is looser than `^`, and it leaves `< c` unparsed instead of reporting the chain.

### Text capture

At character layers, `Slice()` returns a `ReadOnlyMemory<char>` view of the consumed source without copying; `Text()` allocates a `string`. Tokens should normally carry slices.

### Custom primitives

Some tokens need hand-written code. C# raw string literals and Rust's `r#"…"#` strings count their delimiters, which no regular expression can do. `Custom(scanner)` wraps an `IScanner<TIn, TOut>` whose author declares whether it can match empty input and which items it can start with:

```csharp
sealed class RawString : IScanner<char, ReadOnlyMemory<char>>
{
    public ScannerInfo Info => new(Nullable: false, First: CharSet.Of('r'));

    // Must fail without consuming input.
    public bool Scan(ref Cursor<char> c, out ReadOnlyMemory<char> value) { /* hand-written */ }
}

static readonly Parser<char, ReadOnlyMemory<char>> RawStringToken = Custom(new RawString());
```

Analysis trusts the declaration. Debug builds and the tracing interpreter check it on every call and report CRL0206 when a scanner breaks it. Inside a `Longest`, engines run the scanner alongside the DFA whenever the next character is in its declared FIRST set, so a lexer with one custom token can still be fused. `Custom` is a single node kind like any other, so every engine still handles every node (§ Types).

## Layers, streams and pipelines

A `Layer<TIn, TOut>` turns a stream of `TIn` into a stream of `TOut` using ordinary combinators, and a pipeline is a chain of layers ending in a parser. Layers compose associatively, with `Layer.Identity<T>()` as the unit, so a lexer followed by a layout pass is itself just a `Layer<char, Token>`.

A two-layer pipeline works like this:

1. **Source text**, from a string, reader or pipe, supplies characters. Each character's span is its own position.
2. A **buffer** holds characters the lexer may still rewind over.
3. The **lexer layer** (`Layer<char, Tok>`, typically `Longest` plus `skip`) turns characters into tokens.
4. A second **buffer** holds tokens. The parser pulls from it, and a rewind is just an index reset, so the lexer never re-runs.
5. The **parser** (`Parser<Tok, Ast>`) produces a `ParseResult<Ast>`: a value, diagnostics, or both.

Data flows forward only, with one exception: the parser can send mode changes (`SetMode`, `PushMode`, `PopMode`) back to the lexer through a mode channel. Every item carries a span into the source text, so a diagnostic from any layer points at the original characters.

### Defining layers

```csharp
// 1. Repeat an item parser to end of input, skipping trivia between items.
Layer<char, JTok> lexer = Layer.Tokens(item: Token, skip: Whitespace.SkipMany());

// 2. Emit zero or more outputs per step, with layer-local state (see § Feedback).
Layer<Tok, Tok> layout = Layer.Transduce(
    initialState: IndentStack.Root,
    step: LayoutStep,
    atEnd: (stack, span) => stack.CloseAll(span));

// 3. Plain item functions, no parsing.
Layer<Tok, Tok> noComments = Layer.Where<Tok>(t => t.Kind != K.Comment);

// 4. Choose the item parser by mode (see § Feedback).
Layer<char, Tok> modal = Layer.Modal(modes, mode => mode switch
{
    LexMode.Operand  => OperandToken,
    LexMode.Operator => OperatorToken,
});
```

`Layer.Tokens` loops: run `skip`; stop at end of input; run `item`; emit its value. An item parser that can succeed without consuming would loop forever, so analysis rejects it (CRL0003). `Emit<T>` is a small struct holding zero, one or many outputs; synthetic outputs such as INDENT and DEDENT are emitted this way.

### Running pipelines

```csharp
var json = Pipeline.From(JsonLexer.Lexer).Then(JsonGrammar.Document);    // Pipeline<char, JsonValue>

ParseResult<JsonValue> one = json.Parse(text, source: "config.json");

IEnumerable<ParseResult<LogEntry>> many =
    Pipeline.From(LogLexer.Lexer).ParseMany(LogGrammar.Entry, reader);    // lazy, record by record
```

Pipelines are immutable values that can be shared across threads; each `Parse` call owns its own buffers and state.

### Embedded languages

`p.Embed(pipeline)` runs a whole sub-pipeline over the text of a token, such as SQL inside a string literal or a fenced code block inside Markdown. The sub-pipeline's spans are mapped back into the parent source through a `SourceMap`, which also handles escapes that make the token text differ from the raw characters. Its diagnostics land in the same bag as the parent's.

## Cross-layer backtracking and buffering

Each layer boundary is a memoizing stream buffer, so a downstream rewind is just an index reset: upstream layers never re-run, and memory is bounded by the longest backtracking window rather than the input.

### The stream buffer

Each boundary holds a `StreamBuffer<T>`, a segmented, `ArrayPool`-backed buffer indexed by absolute position. Each slot stores five fields:

| Field | Purpose |
| --- | --- |
| `item` | The value the upstream layer produced |
| `span` | Start and end offsets in the original source (§ Source spans) |
| `stepStart` | The upstream position where the step that produced this item began |
| `stateBefore` | A reference to the producing layer's state before that step; null for stateless layers |
| `groupIndex` | This item's index among its step's outputs; 0 unless the step emitted several |

These three step fields let any retained item be produced again: re-run its step from `stepStart` with `stateBefore`, and skip the first `groupIndex` outputs.

The downstream parser holds a cursor, which is just an index. Reading past the filled region pulls the upstream layer for one more item, and that layer may in turn pull from its own upstream.

### Bookmarks and retention

Two kinds of mark remember a position:

- **Pinning bookmarks**, taken by `Atomic`, `Lookahead`, `Not` and `Longest` over non-regular alternatives. These may rewind the cursor, so they keep their slots alive until they exit.
- **Light marks**, taken by `Or`, repetition and `Recover`. They record the cursor position, the mode log's length, the diagnostic count and the layer-state reference, but pin nothing. Under commit semantics `Or` never rewinds consumed input: once an alternative consumes, any failure is committed. Its mark only undoes zero-width effects, such as a `SetMode`, when an alternative fails without consuming.

The buffer may discard every slot below the minimum of the cursor and the oldest pinning bookmark, returning segments to the pool. Each layer in turn pins its own input at the `stepStart` of the oldest output slot it still holds, so any retained item can be produced again. Every mark also snapshots layer-local state and the mode log length (§ Feedback).

### Guarantees

1. **Work once.** Each upstream item is produced once per parse, unless a mode change invalidates it.
2. **Bounded memory.** Memory at a boundary is proportional to the longest window held by a pinning bookmark in the layer above, plus the input of the steps that produced retained items. `Or` and repetition add nothing, so a large document parsed without `Atomic`, `Lookahead` or `Not` keeps a constant number of items per boundary, however deeply its `Or`s nest.
3. **Deterministic parser layers take no pinning bookmarks**, so the token buffer beneath them holds one item of lookahead. Lexer lookahead is separate: a `Longest` DFA backs up to its last accepting state, so the character buffer holds up to the longest such backup (§ Static analysis).
4. **Upstream backtracking is private.** A layer's own `Atomic` and `Longest` happen while it runs one step and are invisible downstream.

Two-pass designs that tokenize everything first hold the whole token list in memory. A Creole pipeline over a multi-gigabyte JSON-lines file needs memory proportional to its largest record plus lookahead.

### Failure positions

Each layer records its furthest failure as a buffer index plus an expectation-set handle, without allocating. A `ParseError` is built only for the failure that is finally reported.

## Feedback between layers

Most context sensitivity belongs inside a single layer, so Creole offers three tools in increasing power and recommends the weakest one that works. Only the third crosses a layer boundary, and it comes with precise invalidation and rollback rules.

| Tool | Use when | Examples |
| --- | --- | --- |
| Split and merge | A downstream parser can reassemble what the lexer split | `>>` in `List<List<int>>` vs shift |
| Layer-local state | The layer can decide from its own history | Template-string brace depth, indentation stack, heredoc terminators |
| Mode channel | Only the downstream parser knows | Regex vs division after `)`, element content vs attributes in JSX-like syntax |

### 1. Split and merge

The lexer emits single `>` tokens, and the parser combines them with `Adjacent(Kind(Gt), Kind(Gt))`, which succeeds only when the two spans touch with no trivia between. This needs no feedback at all; it relies on spans being first-class.

### 2. Layer-local state

A layer can thread an immutable state value through its item parser:

- `GetState<S>()` and `SetState<S>(s)` read and write it.
- `p.Scan<S, R>((value, state, span) => (result, newState))` maps a value and updates state in one analyzable node.

Bookmarks snapshot the state by reference, so backtracking restores it automatically. State must be immutable (`ImmutableStack<int>`, records); the analyzer warns on mutable reference types (CRL0204).

### 3. Mode channels

A `ModeChannel<TMode>` connects an upstream layer to any layer downstream of it. The upstream layer reads it through `Layer.Modal(channel, mode => itemParser)`. The downstream parser writes it with `SetMode`, `PushMode` and `PopMode`, which are zero-width parsers and compose like any other node:

```csharp
static readonly Parser<Tok, Expr> Operand =
    Primary.Between(SetMode(Modes, LexMode.Operand), SetMode(Modes, LexMode.Operator));
```

`Layer.Modal` calls its selector once per distinct mode case and caches the result for the life of the layer. Building nodes inside the lambda, as Example C does, is therefore safe: each mode's item parser is planned once, and plans stay cached by node identity.

**Invalidation rule.** Suppose the downstream cursor is at index *i*, so items 0 to *i* − 1 have been consumed, when it executes a mode change:

1. Every buffered item at index *i* or later was produced under the old mode and is discarded, together with any diagnostics raised while producing it.
2. If item *i* was buffered, its producing step is re-run under the new mode: the upstream cursor returns to the slot's `stepStart`, the layer's state to `stateBefore`, and the first `groupIndex` outputs of the re-run are dropped because they were already consumed. Creole checks that those dropped outputs match the consumed ones. If the new mode changes them, the grammar is rewriting history, and Creole reports CRL0008 instead of silently skipping or duplicating items.
3. The change is appended to the mode log as (index *i*, previous mode stack).
4. Later pulls lex under the new mode.

**Rollback rule.** Each bookmark and light mark records the mode log's length. Rewinding to a bookmark undoes every log entry made after it, newest first. Each undo restores the previous mode stack and applies steps 1 and 2 again at that entry's index.

In practice a change discards zero or one already-lexed items, because the parser rarely looks more than one token ahead. Consumed items are never invalidated: history before index *i* stands.

**Across several layers.** A channel may skip intermediate layers, for example a parser steering the lexer beneath a layout layer. Invalidation then cascades downward: truncating one buffer at index *i* re-runs the step that produced item *i*, and that slot's `stepStart` says where to truncate the buffer below it, down to the channel's producer. Intermediate layers restore their state from `stateBefore` and re-run over the re-lexed items. No storage is needed beyond the slot fields every layer already keeps.

**Mode payloads.** `TMode` can be a union, which makes `Layer.Modal` switches exhaustive. Cases should carry no data: a payload makes the item parser depend on a value, which analysis treats like a `Bind` and fusion cannot compile. Data the lexer can decide by itself, such as a heredoc's terminator, belongs in the lexer's own layer state. Payloads remain possible for the rare case where only the parser knows the data; the item parser reads them with `ModeData<T>()`, and that layer gives up fusion.

## Source spans and provenance

Every item at every layer carries a span into the original source, stored beside the item in the stream buffer rather than inside the user's token type. Users never add position fields to their tokens, and no wrapper objects are allocated.

```csharp
public readonly record struct SourceSpan(SourceId Source, long Start, long End)  // root units
{
    // Offsets in decoded text. Equal to Start and End when the root is already text.
    public long CharStart { get; init; }
    public long CharEnd   { get; init; }
}
```

Offsets are `long` because streams can exceed 2 GB. Buffers still store 32-bit offsets relative to each segment's base, so the wider public type costs nothing per item. When the root is bytes, every span carries both units: byte offsets for tools that seek in the file, and character offsets for line, column and display. Text-rooted pipelines skip the character fields.

### Span rules

1. **Root layer.** Character *i* of the source has span \[*i*, *i* + 1).
2. **Derived items.** An item's span runs from the start of the first input item its item parser consumed to the end of the last one. Input consumed by `skip` is excluded, so a token's span never includes leading whitespace.
3. **Synthetic items.** An item produced without consuming input (INDENT, DEDENT, an inserted semicolon) gets a zero-width span at the start of the next input item and is flagged `IsSynthetic`.
4. **Embedded sources.** Spans from an embedded sub-pipeline are mapped through a `SourceMap` back into the parent source, so they still point at real characters.

Line and column are computed on demand from a `LineMap` built once per source, using binary search over line starts. Streaming parses cannot keep the whole text, so their diagnostics capture positions when they are raised (§ Streaming). Columns are reported in Unicode scalar values by default and rendered with grapheme-aware caret alignment.

### Using spans in grammars

- `p.Spanned()` returns `Parser<TIn, Spanned<TOut>>`; `p.WithSpan((value, span) => node)` builds AST nodes that keep their location.
- `Adjacent(p, q)` succeeds only if `q` starts exactly where `p` ended (§ Feedback).
- `Position` is a zero-width parser returning the current offset.

### Trivia (optional)

`Layer.Tokens(item, skip, trivia: TriviaMode.Attach)` keeps skipped input as leading and trailing trivia spans on each token, in the style of Roslyn's syntax trees. That makes lossless round-tripping possible for formatters and refactoring tools. Trivia is stored as span ranges, not strings, and costs nothing when the mode is off.

## Errors, diagnostics and recovery

Creole reports the furthest failure, merges what was expected there, names the layer that failed and renders the result against the original text. Recovery combinators let one parse produce many diagnostics without cascades.

### The error model

```csharp
public sealed record ParseError(
    SourceSpan Span,                              // where, in original-source terms
    string Layer,                                 // "lexer", "layout", "parser"
    Unexpected Unexpected,
    IReadOnlyList<Expectation> Expected,
    IReadOnlyList<string> Context);               // "in object starting at 2:3"

public sealed record UnexpectedItem(string Display);
public sealed record EndOfInput;
public sealed record CustomMessage(string Text);
public sealed record Label(string Name);
public sealed record ExpectedItem(string Display);

public union Unexpected(UnexpectedItem, EndOfInput, CustomMessage);
public union Expectation(Label, ExpectedItem, EndOfInput);
```

- **Furthest failure wins.** At equal positions, expected sets are merged.
- **Labels.** `p.Named("value")` replaces `p`'s expected set with one label, but only when `p` fails without consuming. A committed failure inside it keeps its precise expectations.
- **Context.** `p.Context("in object")` pushes a line onto the context stack for failures inside `p`.
- **Display.** `TokenGrammar` has an overridable `Describe(TKind)`, so expectations read `','` or `'}'` rather than `Comma` or `RBrace`.

Rendered, an error looks like this:

```text
error[parser]: expected ',' or '}'
 --> config.json:3:16
  |
3 |   "port": 8080 "host": "x"
  |                ^^^^^^ unexpected string "host"
  = note: in object starting at 2:1
```

### Recovery

`p.Recover(strategy, fallback)` catches any failure of `p`, records a diagnostic, repairs the input position and returns `fallback`, often an explicit error node. It never re-runs `p`. Before skipping, it restores the mode stack and layer state that held when `p` started, because a half-parsed construct's mode changes no longer apply. The restore is an ordinary mode change at the current position, so any lookahead lexed under the abandoned mode is invalidated. Strategies follow chumsky's catalogue:

| Strategy | Behavior | Typical use |
| --- | --- | --- |
| `SkipUntil(sync)` | Skip items until `sync` would succeed, without consuming it | Statements up to `;` or a newline |
| `SkipNested(open, close)` | Skip a balanced delimited region | A broken block in a brace language |

**Missing items.** Insertion repairs a single expected item instead of being a `Recover` strategy: `Kind(K.Semicolon).OrInsert(span => new Tok(K.Semicolon, default))`. If the item is missing, it records a diagnostic and succeeds with the token the author built, flagged synthetic, with a zero-width span; nothing is re-run. It inserts only where the failure would otherwise be committed, after the enclosing sequence has consumed something, so it never changes which alternative a choice takes.

Lexer layers recover the same way. `Layer.Tokens(..., onError: Recover.SkipChar(span => new Tok(K.Error, span)))` emits an error token and carries on.

**Cascade suppression.** `Kind(k)` never matches an error token, and a downstream diagnostic whose unexpected item is an error token is dropped, because the lexer already reported it.

**Withdrawal.** Every diagnostic records the layer and item index that raised it. Rewinding a mark withdraws diagnostics raised after it, and mode invalidation withdraws those raised while producing discarded items. A re-lexed token's errors are therefore reported once, from the version that survives.

### Results

`ParseResult<T>` is a union of Parsed, Recovered and Failed (§ Using C# 15 unions). Recovered and Failed carry a `Diagnostics` collection ordered by span. `Parsed<T>` means no errors were recorded. After recovery a value can exist alongside errors, which is what editors and linters want, and the Recovered case makes that state impossible to overlook.

### Limits for untrusted input

`ParseOptions.MaxDepth` bounds recursion depth and `ParseOptions.MaxBufferedItems` bounds each layer's buffer. Exceeding either produces an ordinary diagnostic at the offending span, never a stack overflow; Parlot's recursion limit is the model.

## Static analysis

Because grammars are data, Creole computes classic grammar properties for every node and turns the familiar combinator bugs into errors before any input is parsed. An opt-in deterministic mode goes further and guarantees linear-time parsing.

### Properties

A fixpoint over the grammar graph (through `Rec` cycles) computes, per node:

- **Nullable:** can succeed without consuming.
- **FIRST:** the token kinds or character ranges that can start a match, or ⊤ when an opaque node is reachable first.
- **Regular:** contains no `Rec`, `Bind` or state, so it can compile to a DFA.
- **Deterministic:** satisfies the LL(1) conditions below.

### Diagnostics

| ID | Finding | Severity | Example |
| --- | --- | --- | --- |
| CRL0001 | Repetition of a nullable parser: an infinite loop | Error | `Whitespace.SkipMany().Many()` |
| CRL0002 | Left recursion, reported with the cycle path | Error | `Expr = Rec(() => Seq(Expr, Plus, Term))` |
| CRL0003 | Layer item parser is nullable | Error | `Layer.Tokens(item: Digit.Many())` |
| CRL0004 | A field initializer uses a field declared later without `Rec`, so it reads null at run time | Error | `Value` uses `ArrayValue` declared below it |
| CRL0006 | A construct not allowed in deterministic mode | Error | `Atomic` inside `Grammar.Deterministic(...)` |
| CRL0008 | A mode change altered items already consumed | Error, at run time | A step whose earlier outputs depend on the new mode |
| CRL0101 | Later alternative shadowed by an earlier prefix | Warning | `String("<").Or(String("<="))` |
| CRL0102 | Overlapping FIRST in `Or` without `Atomic`: the later alternative is never tried after a committed failure | Warning | `Seq(Ident, Colon).Or(Seq(Ident, Equals))` |
| CRL0103 | Two `Longest` alternatives match the same string; the earlier wins | Info | Keyword vs identifier rule |
| CRL0105 | `Match` over a union leaves a case without a branch | Error | `V2Header` unhandled in `Header.Match(...)` |
| CRL0106 | A `Longest` DFA can back up over unbounded input, making lexing quadratic | Warning | Tokens `a` and `a*b` on input `aaaa…` |
| CRL0201 | Opaque node limits analysis | Info | `Bind`, `Satisfy` |
| CRL0202 | Dependent query clause makes the rest of the rule opaque | Info | `from n in UInt16 from xs in Any.Repeat(n)` |
| CRL0204 | Mutable type used as layer state | Warning | `List<int>` as indentation stack |
| CRL0205 | A `Reusable` rule's result type stores a `SourceSpan`, which goes stale on reuse | Warning | `record Stmt(SourceSpan Span, ...)` |
| CRL0206 | A `Custom` scanner broke its declared metadata | Error, Debug run time | Declared non-nullable but matched empty input |
| CRL0301 | Node not staged; falls back to the interpreter | Info | Call to a non-inlinable helper |
| CRL0302 | Layers not fused, naming the failing condition | Info | Stateful layer between lexer and parser |
| CRL0401 | Grammar class is not `partial`, so it cannot be staged | Warning | `sealed class JsonGrammar : TokenGrammar<JTok, J>` |
| CRL0501 | Unlabelled rule whose errors would list many raw tokens | Info | An unnamed `OneOf` of 12 keywords |

CRL0004 targets the most common bug in Sprache and Pidgin grammars. The analyzer reads field initializers in declaration order and offers a fix that wraps the reference in `Rec`. At run time, combinators also reject null arguments with a `GrammarException` that names the argument expression.

CRL0101 is decided exactly when both alternatives are regular, using a product of their DFAs. Otherwise it is approximated from FIRST sets.

### Where analysis runs

- **In tests:** `grammar.Analyze()` returns a report, so `Assert.Empty(JsonGrammar.Document.Analyze().Errors)` belongs in every grammar's test suite.
- **On first use in Debug builds:** errors throw a `GrammarException` naming the rule.
- **At build time:** the Roslyn analyzer reports diagnostics in the IDE for grammars the source generator can read.

### Deterministic mode

`Grammar.Deterministic(p)` asserts that `p` is LL(1):

1. The alternatives of every `Or` have disjoint FIRST sets.
2. At most one alternative of any choice is nullable.
3. For every nullable node in a sequence, its FIRST set is disjoint from the FIRST set of what follows it.
4. No opaque nodes are reachable.
5. No `Atomic`, `Lookahead`, `Not`, or `Longest` over non-regular alternatives, since each needs to rewind (CRL0006).
6. Every alternative of a choice starts in the same lexer mode, so the token used to dispatch is lexed under the mode that will be in effect. A mode change shared by all alternatives is hoisted above the choice; differing ones are rejected (CRL0006).

`Recover`, `OrInsert`, `Cut` and mode changes elsewhere remain allowed, because none of them rewinds. If the check passes, engines compile every `Or` to a jump table and take no pinning bookmarks, so parsing is linear in the number of tokens. This mirrors Krishnaswami and Yallop's type system, checked when the grammar is constructed because C#'s type system cannot express it.

The lexer is a separate question. `Longest` over regular alternatives compiles to a DFA and is exempt from rule 1, but maximal munch can back up: with tokens `a` and `a*b`, the input `aaaa…` makes every token scan to the end and retreat, which is quadratic. CRL0106 flags token sets whose backup is unbounded; Reps' memoized tokenization algorithm (1998) keeps maximal munch linear for any that remain.

## Developer experience for grammar authors

Creole treats grammar authors as users with diagnostics of their own: mistakes in a grammar surface at build time, in the IDE, with a fix wherever the fix is mechanical. This is the second half of the diagnostics goal (G10); the first half, errors for the people using a parser, is § Errors.

| Need | What Creole provides |
| --- | --- |
| Catch grammar bugs early | Analyzer diagnostics (§ Static analysis) as you type, for grammars the generator can read |
| Fix them quickly | Code fixes: rewrite left-recursive operators with `Expression.Build`, wrap an overlapping alternative in `Atomic`, wrap a forward field reference in `Rec`, rewrite an independent query as `Seq`, make a grammar class `partial` |
| Understand a grammar error | `GrammarException` names the rule path (`Document → Value → ObjectValue`), never an internal node type |
| See what the parser did | `ParseOptions.Trace` records every attempt: rule, span, outcome and why it failed. Traces render as indented text or export for a viewer |
| Explain an error message | `diagnostic.Explain()` lists the alternatives tried at the furthest position and which `Named` label summarized them |
| Debug generated code | Generated parsers carry `#line` directives back to grammar declarations, so breakpoints on grammar lines work |
| Test grammars | `Creole.Testing`: `Parses`, `FailsAt(line, column, expected)`, snapshot tests of rendered diagnostics, and `EngineAgreement.Check` |
| Fuzz grammars | Grammars are data, so `Creole.Testing` can generate random valid inputs from a grammar and mutate them into near misses |
| Document a language | Export any rule as EBNF or a railroad diagram |
| Design good error messages | CRL0501 flags rules whose errors would list many raw tokens, prompting a `Named` label |

Good end-user diagnostics are designed rather than emergent, so several of these tools exist to help authors design them: `Explain`, CRL0501 and diagnostic snapshot tests together make error quality something a grammar's test suite can check.

## Execution engines

Creole ships two engines with one semantics: an interpreter that runs any grammar anywhere, and a Roslyn source generator that compiles grammars it can read into plain C# at build time. Staging is an optimization, never a dialect: a grammar the generator cannot fully read still works.

|  | Interpreted | Source-generated |
| --- | --- | --- |
| Runs | Any grammar, built at run time or statically | Grammars declared as static fields in `partial` grammar classes |
| Preparation | Lowered to a plan on first use, then cached | At build time; no startup cost |
| Dispatch | Virtual calls on plan nodes | Direct calls, `switch`, inlined lambdas |
| Native AOT | Yes | Yes |
| Fallback | n/a | Unreadable subtrees call into the interpreter |

### Why keep an interpreter

The interpreter is not a stopgap. Four things depend on it:

1. **Grammars built at run time.** Plugin-defined syntax, user-configurable DSLs, keyword tables loaded from data, and REPLs or notebooks where no build step runs.
2. **Fallback.** Any subtree the generator cannot read still runs, so staging never restricts how grammars are written.
3. **Reference semantics.** It is the simplest correct implementation, so it is the oracle the generated and fused engines are tested against (§ Equivalence).
4. **Tooling.** Tracing, `Explain` and grammar fuzzing are cheap in an interpreter and would bloat generated code.

If the generator ever covered every grammar, the interpreter could shrink to a test oracle and a debugging aid, but it would not go away.

### Interpreted engine

On first use, a grammar is lowered to an execution plan and cached in a `ConditionalWeakTable` keyed by node. Each node lowers itself through an internal abstract method (§ Types), so lowering sees every node's types without casts. Plans are immutable and thread-safe; parse state lives in the call. Lowering performs these rewrites:

- Chains of `Select` collapse into one composed function.
- An `Or` over primitives with disjoint FIRST sets becomes a `switch` on token kind or character.
- Regular character subtrees compile to DFA tables.
- Runs of a character class (`Letter.SkipMany()`, identifiers, whitespace) become a `SearchValues<char>` scan with `IndexOfAnyExcept`, which the runtime vectorizes.

Plan nodes are sealed classes with `abstract bool TryParse(ref Cursor<TIn> c, ref TOut result)`. `Cursor<TIn>` is a ref struct with a fast path for reads inside the current buffer segment.

### Source-generated engine

```csharp
// No attribute: deriving from a grammar base class opts in, and `partial` enables staging.
public sealed partial class JsonGrammar : TokenGrammar<JTok, J>
{
    public static readonly Parser<JTok, JsonValue> Value = Rec(() => OneOf(/* ... */));
}

// Generated. The static constructor runs after the field initializers and registers
// compiled code for each rule; engines look it up when planning, so callers keep
// using JsonGrammar.Value and get the compiled parser transparently.
partial class JsonGrammar
{
    static JsonGrammar() => Staging.Register(Value, Parse_Value);
}
```

**Discovery.** The generator finds grammars by base type: any `partial` class deriving from `Grammar<TIn>` is staged, so no attribute is needed. An optional `[GrammarOptions]` attribute tunes staging per class. A grammar class that is not `partial` still runs on the interpreter and gets CRL0401. If the class already declares a static constructor, the generator registers from a module initializer instead. Parlot instead has users declare a `partial` parse method for its generator to implement; registration lets a grammar's own fields stay the entry point.

The generator walks each field initializer with the semantic model, recognizing Creole API calls, and emits one method per rule:

```csharp
private static bool Parse_Value(ref Cursor<JTok> c, out JsonValue result) { /* switch on c.PeekKind() ... */ }
```

- `Seq` becomes straight-line code, `Or` a `switch`, `Many` a loop and `Rec` a direct, possibly mutually recursive, call.
- Non-capturing lambdas passed to `Select` are inlined; the analyzer asks for the `static` modifier to make this explicit.
- Expression-bodied static helper methods in the same compilation, such as `static Parser<char, T> Lexeme<T>(Parser<char, T> p) => p.Before(Ws);`, are inlined like macros, since this idiom is everywhere in combinator code.
- Anything else (a captured local, a helper from another assembly, a grammar built in a loop) is emitted as a call into the interpreter for that subtree, with CRL0301 explaining why.

### Equivalence

Both engines must agree on the value, the consumed position, every diagnostic's span and every expected set. The library's CI runs property-based differential tests over randomly generated grammars and inputs. Users get the same check as `Creole.Testing.EngineAgreement.Check(grammar, inputs)`.

### Considered and deferred

Runtime compilation with `System.Linq.Expressions` or `DynamicMethod` would accelerate grammars built at run time. It is deferred because it conflicts with Native AOT and adds startup cost, but the plan representation leaves room for it as a third engine.

Parlot takes a different route to staging. Its generator executes `[GenerateParser]` methods at build time to build the grammar graph, then emits code for it. That handles grammars assembled by loops and helpers, at the cost of compiling the grammar's dependencies inside the generator, which Parlot asks users to list with `[IncludeFiles]`. Creole reads syntax only, by design: executing user code inside the compiler is fragile in the IDE and works against incremental caching, and anything the generator cannot read still runs on the interpreter.

## Layer fusion

When a lexer layer and the parser directly above it meet the conditions below, the source generator emits one loop in which the parser's decisions read characters directly, and tokens are never stored. This is flap's technique, which its authors report running two to six times faster than ocamlyacc, adapted to C# source generation. It answers the main objection to layering: that every boundary costs a token allocation and a buffer.

### How fusion works

1. The lexer's item parser is a `Longest` over regular alternatives, and its `skip` is regular. Both compile to a single DFA whose accepting states are labelled with token kinds.
2. The parser is in deterministic mode, so each decision point needs only the next token's kind.
3. In the generated parser, "peek the next kind" becomes "run the DFA from the current character", and "consume the token" advances the character position. The DFA's accept dispatch and the parser's `switch` merge into one branch.
4. Token payloads are built only where a `Select` actually uses them, as slices of the source.

The fused DFA always recognizes the full token set with the same longest-match and priority rules. Fusion therefore never changes which token is recognized; it only removes materialization and dispatch. Spans survive as start and end offsets held in locals.

### Conditions

| Condition | Why |
| --- | --- |
| Lexer item is regular: `Longest` of regular alternatives, regular `skip` | It must compile to a DFA |
| Lexer has no layer state, or only a finite mode channel | Each mode becomes its own DFA, selected by the current mode |
| Parser passes `Grammar.Deterministic` | One token of lookahead, no bookmarks across the boundary |
| No intermediate layer, or only `Where` / `Select` item functions | Stateless item functions inline; stateful layers break fusion |

When any condition fails, the generator falls back to unfused per-layer code with a buffer between, and an info diagnostic (CRL0302, alongside CRL0301) names the failing condition. Fusion is a Phase 4 goal and is gated on a measured benefit of at least 1.2× on .NET (§ Roadmap).

## General parsing backend (exploration)

A general, GLL-style backend looks feasible on Creole's grammar tree, but only for a declarative subset of the combinators. It is therefore scoped as a research spike after 1.0, with a decision at the end, rather than as a commitment.

### What it would add

- Ambiguous grammars: all parses returned as a shared packed parse forest, or ambiguity reported as a diagnostic.
- Left recursion written directly, with no `Expression.Build` and no rewriting. For language authors this is the largest usability gain.
- Grammars copied from language specifications, which are rarely LL(1).

### What has to change

| Concern | Deterministic engines | General backend |
| --- | --- | --- |
| Choice | Ordered `Or` with commit | Unordered alternation; every alternative explored |
| `Atomic`, `Cut`, `Not` | Control backtracking | `Not` becomes a follow restriction; `Atomic` and `Cut` have no meaning |
| `Bind`, layer state | Supported | Not supported: alternatives run in parallel, so state cannot be threaded |
| Grammar actions (`Select`) | Run as parsing proceeds | Deferred until a derivation is chosen |
| Errors | Furthest failure | Furthest position reached by any branch; harder to make precise |
| Cost | Linear in deterministic mode | Cubic worst case; near-linear on mostly deterministic grammars |

Layering helps. The lexer stays deterministic, with maximal munch, and only the parser layer goes general. Static analysis helps too: `Or` nodes with disjoint FIRST sets can dispatch deterministically inside the general engine, so the cost concentrates where the grammar is genuinely ambiguous. Meerkat's near-linear behavior on real grammars, and ANTLR 4's adaptive prediction, rest on the same observation.

### Proposed spike

1. Define the declarative subset, and an analyzer that reports when a grammar leaves it.
2. Implement a GLL interpreter over that subset, following Spiewak's and Meerkat's designs, producing a parse forest.
3. Add disambiguation filters: priority, associativity, follow restrictions and preference.
4. Measure against the deterministic engine on JSON and a C-like subset, and on a deliberately ambiguous grammar.
5. Decide among a supported backend, an opt-in per rule (general only where marked), or a documented no.

## Streaming and async input

Parsing is synchronous over buffered data; asynchrony lives only at the source. An async driver parses one top-level item at a time and restarts that item if it runs out of data mid-parse.

### Sources

| Source | API | Notes |
| --- | --- | --- |
| `string`, `ReadOnlyMemory<char>` | `Parse`, `ParseMany` | Whole input in memory |
| `TextReader` | `Parse`, `ParseMany` | Blocking reads as layers pull |
| `PipeReader`, `Stream` | `ParseManyAsync` → `IAsyncEnumerable<ParseResult<T>>` | Record streams: JSON lines, logs, protocols |

Byte sources decode through a `Layer<byte, char>`, so decoding is just another layer. Spans then carry both byte offsets into the root source and character offsets into the decoded text (§ Source spans).

**Diagnostics from streams.** A streaming parse discards text behind the bookmark horizon, so it cannot render a diagnostic later. Instead, each diagnostic raised in streaming mode is resolved at once. It captures its line and column, from a line counter the root layer updates as it releases segments, and a snippet of the offending line capped at a configurable length. Whole-input parses still resolve lazily.

### The restart protocol

1. The driver takes a bookmark at the start of the next item, across every layer, including layer states, mode logs and the diagnostic bag, so a starved attempt leaves no stray diagnostics.
2. It runs the item parser synchronously over the data currently buffered.
3. If a root read reaches the end of buffered data before the true end of the stream, the attempt ends **Starved**.
4. The driver awaits more data, rewinds every layer to the bookmark and retries.

Starved is not a failure. `Or`, `Atomic`, `Not`, `Lookahead` and `Recover` all treat it as an abort that propagates unchanged, because trying another alternative on incomplete data would produce a wrong parse.

**Cost.** Before retrying, the driver at least doubles the buffered data beyond what the failed attempt saw. The total re-parsing work per item is then a geometric series, linear in the item's size.

### Why not async combinators

An `async` state machine per combinator call costs far more than a parse step, and most items fit in one buffer. Two cheaper routes cover the rest:

- **Huge single documents (1.0).** Run the synchronous parser on a dedicated thread over a blocking `TextReader` that wraps the pipe. This costs one thread per parse and needs no design change.
- **Resumable generated parsers (1.x candidate).** The generator could emit each rule as a state machine that suspends at the exact read that ran dry. That removes restarts and the extra thread, but it forces parse state onto the heap, adds suspension checks to the common path and enlarges generated code. It would be offered for the generated engine only, and only if it passes its gate (§ Roadmap).

## Incremental re-parsing

After an edit, Creole re-runs only the layer work the edit can affect, and reuses whole rule results where the author opts in. This serves editors and language servers (G12) and is planned for 1.x, once the 1.0 engines are stable.

### Retained parses

`pipeline.ParseIncremental(text)` returns a `ParseSession` that keeps every layer's buffer instead of discarding behind the bookmark horizon. `session.Edit(span, newText)` produces the next `ParseResult`. Memory is proportional to the document, which is acceptable for open editor buffers.

### Reusing layer items

Each buffered item records one more number: how far its producer *examined* input, including lookahead and failed `Longest` alternatives, not just what it consumed. After an edit:

1. Items whose examined range ends before the edit are reused unchanged.
2. Re-lexing starts at the first item whose examined range touches the edit.
3. Re-lexing stops as soon as a new item ends exactly where an old item ended, after shifting by the edit's change in length, with the same layer state and mode. From there the old items are reused, and their spans shift lazily through an offset table rather than being rewritten.

The same rule applies at every layer, so a one-character edit inside a string literal re-lexes one token, re-runs the layout layer over one item and stops.

### Reusing parser results

Parser rules opt in with `.Reusable()`, typically statements, members and blocks. A reusable rule's result is memoized with its start and end indexes, its examined extent and the layer state on entry. On re-parse, an entry whose examined range is untouched and whose entry state matches is reused, and the parser skips ahead. This is the technique of incremental packrat parsing (Dubroy and Warth, SLE 2017), applied to selected rules rather than every expression.

Reused results are the same objects as before, so absolute positions stored inside them would be stale, and Creole cannot look inside user node types at run time. It does not ask users to adopt a red/green tree either. Instead, a session keeps positions on the side: `WithSpan` records each node's span in a table owned by the session, keyed by node identity, which shifts spans through the same offset table as layer items. `session.SpanOf(node)` returns a node's current span, so tracked nodes must be reference types. The check that Creole cannot make at run time happens at build time: CRL0205 warns when a `Reusable` rule's result type stores a `SourceSpan`.

### Limits

- A reusable rule that issues mode changes is reused only when its entry mode stack matches; `Bind` inside a reusable rule disables reuse for that rule.
- Items produced by error recovery are never reused, so errors are always diagnosed afresh.

## Performance design and benchmark plan

The interpreted engine must be at least as fast as Pidgin and within 1.5× of Parlot, and the generated engine must match Parlot and should approach hand-written recursive descent. These are gates to measure, not claims (§ Roadmap).

### Design choices

- **Struct tokens with enum kinds**, so dispatch is a `switch` and tokens never box.
- **Out-of-band spans**: 32-bit offsets relative to the segment base, stored beside each buffered item (two per item, four when the root is bytes) and widened to the public `long` `SourceSpan` only when read; no wrapper objects (§ Source spans).
- **Pooled, segmented buffers** released at the bookmark horizon.
- **Lazy errors**: failures record a position and an interned expectation-set handle; a `ParseError` object exists only for the reported failure.
- **Vectorized character classes** through `SearchValues<char>`.
- **Slices, not strings**: `Slice()` returns `ReadOnlyMemory<char>`; `Text()` is opt-in.
- **Value-type results** flow through generic plan nodes without boxing; skipped results use `Unit`.
- **No delegates on the hot path** in generated code.

### Benchmark plan

All benchmarks use BenchmarkDotNet with the memory diagnoser, and run each Creole engine (interpreted, generated, fused where eligible). Gate runs use one machine with pinned settings and run the suite three times; the gate thresholds are in § Roadmap. The memory diagnoser reports bytes allocated, not retained, so the Phase 1 memory gate samples retained heap in its own test.

| Benchmark | Layers | Compared against | Primary measure |
| --- | --- | --- | --- |
| JSON documents, small to large | 2 | Parlot, Pidgin, Superpower, Sprache; System.Text.Json as a ceiling | Throughput (MB/s), allocations |
| Arithmetic expressions | 2 | Parlot, Pidgin, hand-written recursive descent | Time per expression |
| Indentation-sensitive mini-language | 3 | Hand-written lexer and parser | Throughput |
| Large JSON-lines stream | 2, streaming | Superpower's tokenize-then-parse | Peak memory |
| Inputs with many errors | 2 | Pidgin, Superpower | Diagnostics count and quality (reviewed by hand), time |

The Sprache comparison doubles as a migration guide: each benchmark grammar is written in both libraries side by side.

## Worked examples

Three grammars show the design end to end: JSON in two layers, an indentation-sensitive language in three, and a script language whose parser steers its lexer.

### A. JSON in two layers

The lexer uses `Longest` over regular alternatives, so it is eligible for DFA compilation and, with the deterministic parser, for fusion.

```csharp
public enum J { LBrace, RBrace, LBracket, RBracket, Colon, Comma, String, Number, True, False, Null }

public readonly record struct JTok(J Kind, ReadOnlyMemory<char> Text) : IKinded<J>;

public union JsonValue(JsonString, JsonNumber, JsonBool, JsonNull, JsonArray, JsonObject);

public sealed partial class JsonLexer : CharGrammar
{
    // Expression-bodied helper: the generator inlines it.
    static Parser<char, JTok> Tok<T>(Parser<char, T> p, J kind) =>
        p.Slice().Select(text => new JTok(kind, text));

    static readonly Parser<char, Unit> StringBody =
        OneOf(Seq(Char('\\'), Any).Ignore(), NoneOf("\"\\").SkipAtLeastOnce()).SkipMany();

    static readonly Parser<char, Unit> Number =          // simplified: no exponent
        Seq(Char('-').Optional(),
            Digit.SkipAtLeastOnce(),
            Seq(Char('.'), Digit.SkipAtLeastOnce()).Optional()).Ignore();

    public static readonly Layer<char, JTok> Lexer = Layer.Tokens(
        item: Longest(
            Tok(Char('{'), J.LBrace),    Tok(Char('}'), J.RBrace),
            Tok(Char('['), J.LBracket),  Tok(Char(']'), J.RBracket),
            Tok(Char(':'), J.Colon),     Tok(Char(','), J.Comma),
            Tok(Seq(Char('"'), StringBody, Char('"')), J.String),
            Tok(Number, J.Number),
            Tok(String("true"), J.True), Tok(String("false"), J.False), Tok(String("null"), J.Null)),
        skip: Whitespace.SkipMany());
}

public sealed partial class JsonGrammar : TokenGrammar<JTok, J>
{
    public static readonly Parser<JTok, JsonValue> Value = Rec(() => OneOf(
        Kind(J.String).Select<JsonValue>(static t => new JsonString(Unescape(t.Text))),
        Kind(J.Number).Select<JsonValue>(static t => new JsonNumber(
            double.Parse(t.Text.Span, CultureInfo.InvariantCulture))),
        Kind(J.True).As<JsonValue>(JsonBool.True),
        Kind(J.False).As<JsonValue>(JsonBool.False),
        Kind(J.Null).As<JsonValue>(JsonNull.Instance),
        ArrayValue,
        ObjectValue)).Named("value");

    static readonly Parser<JTok, JsonValue> ArrayValue =
        Value.SepBy(Kind(J.Comma))
             .Between(Kind(J.LBracket), Kind(J.RBracket))
             .Select<JsonValue>(static items => new JsonArray(items));

    static readonly Parser<JTok, JsonValue> ObjectValue =
        Seq(Kind(J.String), Kind(J.Colon), Value)
            .Select(static (k, _, v) => KeyValuePair.Create(Unescape(k.Text), v))
            .SepBy(Kind(J.Comma))
            .Between(Kind(J.LBrace), Kind(J.RBrace))
            .Select<JsonValue>(static members => new JsonObject(members));

    // Every alternative of Value starts with a distinct kind, so the LL(1) check passes.
    public static readonly Parser<JTok, JsonValue> Document =
        Grammar.Deterministic(Value.Before(End));
}

var json = Pipeline.From(JsonLexer.Lexer).Then(JsonGrammar.Document);
var result = json.Parse(File.ReadAllText(path), source: path);
if (result is not Parsed<JsonValue>) Console.Error.WriteLine(result.Diagnostics.Render());
```

### B. An indentation-sensitive language in three layers

The middle layer turns indentation widths into INDENT and DEDENT tokens, as Python's tokenizer does. It is an ordinary layer with immutable state, so backtracking above it needs no special care.

```csharp
public readonly record struct Tok(K Kind, ReadOnlyMemory<char> Text, int Width = 0) : IKinded<K>;

public sealed partial class Layout : TokenGrammar<Tok, K>
{
    public static readonly Layer<Tok, Tok> Pass = Layer.Transduce(
        initialState: IndentStack.Root,                                    // immutable stack, [0]
        step: OneOf(
            // Emits NEWLINE, then INDENT or DEDENTs as zero-width synthetic tokens.
            // An inconsistent dedent emits an Error token for the parser's recovery to skip.
            Kind(K.Newline).Scan<IndentStack, Emit<Tok>>(
                static (nl, stack, span) => stack.Adjust(nl.Width, span)),
            Any.Select(static t => Emit.One(t))),
        atEnd: static (stack, span) => stack.CloseAll(span));
}

public sealed partial class MiniGrammar : TokenGrammar<Tok, K>
{
    static readonly Parser<Tok, Stmt> Statement = Rec(() => OneOf(IfStmt, WhileStmt, Assignment));

    static readonly Parser<Tok, Block> Suite =
        Seq(Kind(K.Colon), Kind(K.Newline), Kind(K.Indent), Statement.AtLeastOnce(), Kind(K.Dedent))
            .Select(static (_, _, _, body, _) => new Block(body));

    static readonly Parser<Tok, Stmt> IfStmt =
        Seq(Kind(K.If), Exp, Suite).Select(static (_, cond, body) => (Stmt)new If(cond, body));

    // Exp, WhileStmt and Assignment elided.

    public static readonly Parser<Tok, Module> Root =
        Statement.Many().Before(End).Select(static s => new Module(s));
}

var mini = Pipeline.From(MiniLexer.Lexer).Then(Layout.Pass).Then(MiniGrammar.Root);
```

A DEDENT's span is zero-width at the start of the first token on the dedented line, so "expected statement" errors after a block point at that line.

### C. A parser that steers its lexer

In JavaScript-like syntax, `/` after `)` can start a regex (`if (ok) /x/.test(s)`) or divide (`(a) / 2`), and only the parser knows which. A mode channel settles it.

```csharp
public enum LexMode { Operand, Operator }

public sealed partial class ScriptLexer : CharGrammar
{
    // A channel is a typed key with an initial mode; the mode stack lives in each parse's state.
    public static readonly ModeChannel<LexMode> Modes = new(LexMode.Operand);

    public static readonly Layer<char, Tok> Lexer = Layer.Modal(Modes, static mode => mode switch
    {
        LexMode.Operand => Longest(Common, RegexLiteral),        // '/' starts a regex
        _               => Longest(Common, Slash, SlashEquals),  // '/' divides
    }, skip: Trivia);
}

public sealed partial class ScriptGrammar : TokenGrammar<Tok, K>
{
    static readonly ModeChannel<LexMode> Modes = ScriptLexer.Modes;

    // Before an operand '/' is a regex; after one it is division.
    static readonly Parser<Tok, Expr> Operand =
        Primary.Between(SetMode(Modes, LexMode.Operand), SetMode(Modes, LexMode.Operator));

    static readonly Parser<Tok, Stmt> IfStmt =
        Seq(Kind(K.If), Exp.Between(Kind(K.LParen), Kind(K.RParen)),
            SetMode(Modes, LexMode.Operand),                  // a statement starts here
            Statement)
        .Select(static (_, cond, _, body) => (Stmt)new If(cond, body));

    // Primary, Exp (built with Expression.Build over Operand) and Statement elided.
}
```

In `(a) / 2`, the operand `(a)` ends in Operator mode, so `/` lexes as division. In `if (ok) /x/.test(s)`, `IfStmt` switches to Operand mode after `)`, so `/x/` lexes as a regex. If the lexer had already looked ahead under the old mode, the invalidation rule discards that one token and re-lexes it.

## Comparison with existing .NET libraries

Creole's differences are concentrated in three rows: stage count, feedback and analysis. Parlot already generates code at build time, so Creole's generator is a means, not a differentiator. On combinator vocabulary and commit semantics Creole deliberately follows Pidgin.

| Capability | Sprache | Superpower | Pidgin | Parlot | Creole (proposed) |
| --- | --- | --- | --- | --- | --- |
| Parser representation | Delegate | Delegate | Class with a parse method | Class with a parse method | Inspectable grammar tree |
| Input type | `char` | `char`, then `Token<TKind>` | Any `TToken` | Text | Any `T` |
| Stages | One | Two, fixed: tokenizer, then token-list parser | One per run; chained by hand | One; `Terms` skip whitespace and comments | Any number, as a streaming pipeline |
| Default choice | `Or` backtracks; `XOr` commits | Commits; `Try` backtracks | Commits; `Try` backtracks | Failed parsers restore the cursor | Commits; `Atomic` backtracks |
| Parser-to-lexer feedback | None | None | None | None | Layer state and mode channels with rollback |
| Source positions | Opt-in (`Positioned`) | Spans on tokens | Positions in errors | Offsets in `Then` callbacks; positions in errors | Spans on every item at every layer, bytes and characters |
| Static grammar analysis | None | None | None | Lookup tables for `OneOf` from `ISeekable` | Build-time and construction-time checks |
| Build-time code generation | None | None | None | Executes grammar methods, implements declared `partial` methods (preview packages only) | Reads grammar syntax; fusion later |
| Incremental re-parsing | None | None | None | None | Planned for 1.x |

Migration from Pidgin should be mostly mechanical: `Try` becomes `Atomic`, `Map((a, b) => ..., p, q)` becomes `Seq(p, q).Select(...)`, and `using static Pidgin.Parser` becomes a grammar base class. Migration from Sprache is larger, mainly because `Or` changes from backtracking to committing; analyzer CRL0102 flags most places where that matters.

## Roadmap

Creole 1.0 ships after Phase 3, once both engines agree; incremental re-parsing, fusion and resumable async follow in 1.x releases, each behind its own gate. Fusion ships only if it beats unfused generated code on .NET by at least 1.2×.

| Phase | Scope | Gate to pass |
| --- | --- | --- |
| 0. Core | Combinator set, interpreter, error model | Core tests green; interpreter at least as fast as Pidgin and within 1.5× of Parlot |
| 1. Layers | Pipelines, buffers and spans, mode channels | Three-layer demo runs; peak retained memory grows at most 1.5× from 10 MB to 1 GB of input |
| 2. Analysis | Analyzers, deterministic mode, recovery | Every CRL error and warning caught at build time, with no false positives on near misses |
| 3. Staging | Source generator, helper inlining, engine agreement | Engines agree on 10,000 random cases; generated engine as fast as Parlot; **1.0 ships** |
| 4. 1.x releases | Incremental re-parsing, layer fusion, resumable async | Each ships behind its own gate (below) |

Alongside the phases, a research spike explores a general (GLL) parsing backend over a declarative subset (§ General parsing backend).

Each phase is usable on its own: Phase 0 alone is a modern single-layer library, and every later phase adds capability without changing grammars written earlier.

Every gate is a number ([ADR-0003](work/docs/decisions/0003-numeric-gate-thresholds.md)). Speed is measured as a ratio to the baseline in the same BenchmarkDotNet run, taking the median of three runs on one fixed machine, and summarized as a geometric mean over the gate's benchmarks. The full thresholds:

| Gate | Threshold |
| --- | --- |
| Phase 0 speed | Interpreter on character-level JSON and arithmetic: geometric mean ≤ 1.0× Pidgin, no benchmark above 1.25× Pidgin, geometric mean ≤ 1.5× Parlot; allocations ≤ Pidgin's on every benchmark |
| Phase 1 memory | Example B and a JSON-lines stream at 10 MB, 100 MB and 1 GB: peak retained heap at 1 GB ≤ 1.5× the peak at 10 MB |
| Phase 2 analysis | A corpus grammar that triggers each CRL error and warning, and a near miss that does not: 100% caught, 0 false positives |
| Phase 3 agreement | 0 mismatches over 10,000 fixed-seed random grammar and input pairs on every CI run |
| Phase 3 speed | Generated engine on the Phase 0 benchmarks: geometric mean ≤ 1.0× Parlot |
| Incremental re-parsing | 500 or more single-character edits on a file of about 300 KB and on a 10× copy: median ≤ 2% of a full parse, p99 ≤ 100 ms, median on the 10× file ≤ 2× the base file |
| Layer fusion | Geometric mean ≥ 1.2× faster than the best unfused generated code, and no benchmark slower |
| Resumable async | ≥ 1.2× the restart driver's throughput when the median record fills a pipe segment, and ≥ 0.95× on smaller records |

Parlot means stable Parlot's interpreted parsers until `Parlot.SourceGenerator` ships a stable release; the Phase 3 speed gate then compares against Parlot's generated parsers.

## Risks and open questions

The largest risks are the cost of maintaining two engines and the subtlety of mode rollback; both are addressed with testing rather than hope. The largest strategic risk is that Parlot already covers speed and build-time generation.

### Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Two engines drift apart | Wrong results in production only | Differential property tests are a release gate, not a nice-to-have |
| Mode rollback has corner cases, especially across several layers | Silent mis-lexing after backtracking | Model-based tests against a reference re-lexer, including multi-layer cascades |
| Generator authoring rules feel restrictive | Users avoid staging | Interpreter fallback with CRL0301 and helper inlining; measure the fallback rate on real grammars |
| Fusion gains less on .NET than in OCaml | 1.x effort wasted | The gate requires at least 1.2× over unfused generated code. Parlot reports its 2.0 generated parsers 8–23% faster than its runtime ones, and `Compile()` 1–13% faster, a modest prior |
| Parlot already offers fast, build-time-generated combinators | Speed alone does not differentiate Creole | Lead with layers, analysis, diagnostics and incremental parsing; gate speed on Parlot as well as Pidgin (§ Roadmap) |
| C# 15 union details change around GA | Churn in results and errors | Unions confined to a few public shapes; hot paths use hand-written structs |
| Commit-by-default surprises Sprache users | Migration friction | CRL0102 warnings and a migration guide |
| The working name was taken: the `Strata` NuGet ID belongs to an Orleans event-sourcing library, and a `Strata.*` package family exists | No usable package ID; search confusion | Resolved: renamed to Creole, which is free on NuGet. Reserve the package ID before Phase 0. |

### Questions raised in review

- **Multi-hop mode channels.** Can a channel skip an intermediate layer if that layer stores a state snapshot per item? The memory cost may be acceptable for layout-style layers. Decided: adopted, and no extra storage is needed (§ Feedback).
- **Incremental re-parsing.** Buffers already record span and upstream position per item. Could an edit invalidate a span range and resume from the nearest bookmark-free item, as editors need? Decided: adopted as a 1.x goal (§ Incremental re-parsing).
- **Resumable async.** Should the generator emit resumable state machines so `ParseManyAsync` never restarts an item? Decided: not in 1.0. A dedicated-thread option covers huge documents, and resumable generated parsers are a 1.x candidate (§ Streaming).
- **Span units for byte sources.** Byte offsets are exact but awkward for users. Should `SourceSpan` also carry a character offset when a decoding layer exists? Decided: yes, spans carry both (§ Source spans).
- **General parsing backend.** Is a GLL or derivatives engine for ambiguous grammars worth building on the same grammar tree, with parse forests as the result type? Decided: explore in a post-1.0 spike (§ General parsing backend).
- **Sprache adapter.** Is a `Creole.SpracheCompat` package that maps Sprache's API onto Creole worth the maintenance? Decided: no.
- **`ParseResult<T>` as a union or a closed hierarchy.** A union's `Value` is null in a `default` instance, so switches might need a null arm. Decided: a plain union. On the .NET 11 RC 2 compiler a switch on a returned union needs no null arm, and a hand-written `[Union]` struct or a closed hierarchy fixes nothing that matters ([ADR-0002](work/docs/decisions/0002-parseresult-as-a-plain-union.md)).

### Still open

- **Closed enums for token kinds.** If closed enums ship, switches over `TKind` become exhaustive as well. Decided: wait for the language and adopt closed enums when they ship; no API change is needed.
- **A build-time execution tier.** Should the generator execute grammar construction, as Parlot does, for subtrees it cannot read? Decided: no. The generator reads syntax only, and anything it cannot read runs on the interpreter.

## References

**Research**

1. Leijen, Meijer. Parsec: Direct Style Monadic Parser Combinators for the Real World. 2001. [PDF](https://www.cs.tufts.edu/~nr/cs257/archive/daan-leijen/parsec.pdf)
2. Spiewak. Generalized Parser Combinators. 2010. [gll-combinators](https://index.scala-lang.org/djspiewak/gll-combinators)
3. Might, Darais, Spiewak. Parsing with Derivatives: A Functional Pearl. ICFP 2011. [PDF](https://matt.might.net/papers/might2011derivatives.pdf)
4. Izmaylova, Afroozeh, van der Storm. Practical, General Parser Combinators. PEPM 2016. [CWI](https://ir.cwi.nl/pub/25145/)
5. Adams, Hollenbeck, Might. On the Complexity and Performance of Parsing with Derivatives. PLDI 2016. [PDF](https://michaeldadams.org/papers/derivatives2/derivatives2.pdf)
6. Krishnaswami, Yallop. A Typed, Algebraic Approach to Parsing. PLDI 2019. [PDF](https://www.cl.cam.ac.uk/~jdy22/papers/a-typed-algebraic-approach-to-parsing.pdf)
7. Mokhov, Lukyanov, Marlow, Dimino. Selective Applicative Functors. ICFP 2019. [DOI](https://doi.org/10.1145/3341694)
8. Willis, Wu, Pickering. Staged Selective Parser Combinators. ICFP 2020. [ICFP page](https://icfp20.sigplan.org/details/icfp-2020-papers/20/Staged-Selective-Parser-Combinators)
9. Darragh, Adams. Parsing with Zippers (Functional Pearl). ICFP 2020. [ACM](https://dl.acm.org/doi/10.1145/3408990)
10. Willis, Wu. Design Patterns for Parser Combinators (Functional Pearl). Haskell Symposium 2021. [PDF](https://www.cs.tufts.edu/~nr/cs257/archive/jamie-willis/parsing-patterns.pdf)
11. Yallop, Xie, Krishnaswami. flap: A Deterministic Parser with Fused Lexing. PLDI 2023. [arXiv](https://arxiv.org/abs/2304.05276)
12. Dubroy, Warth. Incremental Packrat Parsing. SLE 2017. [SLE page](https://conf.researchr.org/details/sle-2017/sle-2017-papers/16/Incremental-Packrat-Parsing)

**Libraries**

- [Sprache](https://github.com/sprache/Sprache), [Superpower](https://github.com/datalust/superpower) and [Pidgin](https://github.com/benjamin-hodgson/Pidgin) (.NET)
- [chumsky](https://docs.rs/chumsky) (Rust): error-recovery strategies
- FastParse (Scala): cuts
- [Parlot](https://github.com/sebastienros/parlot) (.NET): build-time generation, recursion limits; [combinator reference](https://github.com/sebastienros/parlot/blob/main/docs/parsers.md)
- [Yoakke](https://github.com/LanguageDev/Yoakke) (.NET): attribute-driven lexer and parser generation
