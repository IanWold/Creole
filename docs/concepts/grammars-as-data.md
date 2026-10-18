# Grammars as data

A Creole parser is a value that describes a grammar, not code that parses. Combinators build an immutable tree of nodes. Nothing reads input until an engine runs the tree. The design reasons are in the [whitepaper's § Core model](../../whitepaper.md#core-model-parsers-as-data).

## The parser type

`Parser<TIn, TOut>` reads items of type `TIn` and produces a `TOut` when it succeeds.

- `TIn` is `char` for a character layer, or your token type for a parser layer.
- `TOut` is the value the rule builds. Rules that build nothing produce `Unit`.

## Parsers are immutable and compared by reference

- A parser never changes after it is built, so it can be shared between threads.
- Two parsers are equal only if they are the same object. Building the same rule twice gives two different parsers, so store each rule once, for example in a `static readonly` field.

## You cannot derive from `Parser<TIn, TOut>`

Every node type is defined inside Creole, so engines know every node they can meet. The constructor is internal, so a class outside Creole cannot derive from `Parser<TIn, TOut>`.

## Tokens with kinds

A token type can implement `IKinded<TKind>` to expose an enum kind. Make tokens structs so they never box:

```csharp
public enum J { String, Number, LeftBrace, RightBrace }

public readonly record struct JTok(J Kind, ReadOnlyMemory<char> Text) : IKinded<J>;
```

## `GrammarException`

Building an invalid grammar throws a `GrammarException`, whose message says what is wrong.
