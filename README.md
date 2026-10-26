<div align="center">

# Creole

[![Creole Nuget Version](https://img.shields.io/nuget/v/Creole?style=for-the-badge&logo=nuget&label=Creole)](https://www.nuget.org/packages/Creole)
[![Creole.Testing Nuget Version](https://img.shields.io/nuget/v/Creole.Testing?style=for-the-badge&logo=nuget&label=Creole.Testing)](https://www.nuget.org/packages/Creole.Testing)

A new parser-combinator library for .NET

_Very much in-development: there's a whitepaper and most of a parser... so far..._

[Whitepaper](whitepaper.md) • [Roadmap](work/README.md) • [Documentation](docs/README.md)

</div>

---

I am a big fan of parser-combinators; they're fun and expressive and just computer-sciency enough to make me feel really smart for knowing about them! .NET already has a few great libraries for these (I am very bad at being a member of [Sprache](https://github.com/sprache/Sprache)), but there's been a lot of research in the space in recent years.

I'm going to take a stab at building a super-duper, modern parser-combinator library. Something with a bunch of nerdy bells and whistles that can be used for any grammar. I'm calling it Creole because that's what came to mind (one of the existing libraries is the excellent [Pidgin](https://github.com/benjamin-hodgson/Pidgin)).

In particular, I really want to be able to write a multi-stage parser (lex, token, parse, so on) with one common vocabulary, and chain them together. Issues at any stage should back-propagate, error messages should be a first-class concern throughout, the parser should be able to steer the lexer, and source generation and Roslyn analyzers should be used at every point to make the parser author's devex as smooth as possible.

The plan is a [whitepaper](whitepaper.md) and about 130 work items, which I'm aware is a funny thing to publish for someone who [tells people to descope their personal projects](https://ian.wold.guru/Posts/successful_personal_projects.html). In my defense, each phase stands on its own, and the first one is now done: Creole is a working single-layer parser-combinator library whose interpreter runs about four times as fast as Pidgin on the benchmarks. Everything after that is a bonus.

I'm also using this as a test bed for using Claude to assist with project management and research. I'm a fan of [nockawa](https://github.com/nockawa)'s setup on his [Typhon](https://github.com/Log2n-io/Typhon) project, and I like the idea of all my cards living in markdown in my repo instead of on GitHub. So, we'll be seeing how that goes as well.

## Show Me the Code

Grammars are classes that collect parsers; parsers are static fields therein. Here's one for arithmetic:

```csharp
using System.Globalization;
using Creole;

public sealed class Calculator : CharGrammar
{
    private static readonly Parser<char, Unit> Space = Whitespace.SkipMany();

    private static readonly Parser<char, double> Number = Token(Digit.SkipAtLeastOnce().Slice())
        .Select(digits => double.Parse(digits.Span, CultureInfo.InvariantCulture));

    private static readonly Parser<char, double> Expr = Expression.Build(
        Number.Or(Rec(() => Expr).Between(Token(Char('(')), Token(Char(')')))),
        op =>
        [
            [op.Prefix(Token(Char('-')), (_, x) => -x)],
            [op.InfixL(Token(Char('*')), (l, _, r) => l * r), op.InfixL(Token(Char('/')), (l, _, r) => l / r)],
            [op.InfixL(Token(Char('+')), (l, _, r) => l + r), op.InfixL(Token(Char('-')), (l, _, r) => l - r)],
        ]
    );

    public static readonly Parser<char, double> Document = Space.Then(Expr).Before(End);

    private static Parser<char, T> Token<T>(Parser<char, T> parser) => parser.Before(Space);
}
```

Running it gives you a `ParseResult<T>`, which is a C# 15 union, so the compiler makes sure you handle every case:

```csharp
var result = Calculator.Document.Parse("2 * (3 + 4", source: "input");

Console.WriteLine(result switch
{
    Parsed<double> parsed => $"{parsed.Value}",
    Recovered<double> recovered => recovered.Diagnostics.Render(),
    Failed failed => failed.Diagnostics.Render(),
});
```

```text
error[parser]: expected digit, whitespace, '*', '/', '+', '-' or ')'
 --> input:1:11
  |
1 | 2 * (3 + 4
  |           ^ unexpected end of input
```

(That list of expectations is a bit noisier than I'd like; there's [an idea](work/ideas/hide-a-tokens-internal-expectations.md) to fix that)

Each of those fields is an immutable tree of grammar nodes rather than a delegate, so Creole can look at a grammar before it runs it. Right now the interpreter uses that to lower the tree to a cached plan, compile regular `Longest` choices to automata, and throw a `GrammarException` for a grammar that isn't valid. Later phases will analyze the same tree at build time and compile it to C#.

## Current State

Phase 0 is the single-layer library:

| Area | What is there |
| --- | --- |
| Grammars | `CharGrammar` for text, `TokenGrammar` for tokens, or `Parser<TIn, TOut>` over any input type. `Rec` for recursive and named rules. |
| Combinators | Primitives and character classes; sequencing, mapping and LINQ query syntax; ordered choice with commit semantics, `Longest` (maximal munch), `Switch`, and `Match` over unions; repetition; `Atomic`, `Lookahead`, `Not` and `Cut`; `Expression.Build` for operator tables; custom scanners. |
| Interpreter | `Parse` over text or tokens in memory, thread-safe, with no allocation beyond the result and what the grammar builds. A depth limit stops deeply nested input. |
| Errors | The furthest failure, labelled with `Named` and `Context`, rendered against the source with lines and columns. |
| Native AOT | Safe for Native AOT and trimming: CI publishes and runs a Native AOT app that uses the library. |

What's not here yet: multi-layer pipelines, streaming input, error recovery, build-time analysis, and the source generator - those are Phases 1 through 3.

### Performance

Phase 0 ensured the interpreter passed a speed gate: the interpreter was measured against Pidgin 3.5.1 and Parlot 2.0.0's interpreted parsers on character-level JSON (three sizes) and arithmetic. Each figure is Creole's time divided by the other library's, taking the median of three BenchmarkDotNet runs on my crappy laptop as a geometric mean over the benchmarks:

| Compared with | Gate | Measured |
| --- | --- | --- |
| Pidgin | ≤ 1.0× | 0.24× |
| Parlot | ≤ 1.5× | 1.37× |

Creole also allocated no more than Pidgin on every benchmark. Parlot is still faster (the widest gap is large JSON, at 1.70x). We'll look to optimizer further later when implementing the source generator. The full results are in [BENCH-01](work/active/13-benchmarks/BENCH-01-phase-0-speed-gate.md), and [benchmarks/README.md](benchmarks/README.md) explains how to run them yourself.

## Roadmap

| Phase | Adds | Status |
| --- | --- | --- |
| 0. Core | Combinators, interpreter, error model | ✅ Done |
| 1. Layers | Multi-layer streaming pipelines, cross-layer backtracking, mode channels, streaming input | Next |
| 2. Analysis | Build-time analyzers with code fixes, deterministic mode, error recovery | Planned |
| 3. Staging | Source-generated engine that agrees with the interpreter; 1.0 ships | Planned |
| 4. 1.x | Incremental re-parsing, layer fusion, resumable async | Planned |

Every phase has a numeric gate it needs to pass in ([whitepaper § Roadmap](whitepaper.md#roadmap)), and the [work tracker](work/README.md) has all the cards.

## Using Creole

```
dotnet add package Creole --version 0.1.0-alpha-2
```

Creole targets .NET 11 and uses C# 15 unions in its public API. .NET 11 is a release candidate until it ships, which should be November 2026, so the repo pins the RC 2 SDK in [global.json](global.json):

## Building from Source

```sh
git clone https://github.com/IanWold/Creole.git
cd Creole
dotnet build
dotnet test
```

From there you can reference `src/Creole/Creole.csproj` from any `net11.0` project. The [documentation](docs/README.md) covers the library as it's built so far, and [Running a parser](docs/concepts/parsing.md) is a good place to start.
