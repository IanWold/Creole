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

## Writing a grammar

C# cannot infer the input type from a call such as `Return(1)`. A grammar is therefore a class that derives from a base class fixing the input type. The base class's combinators are then in scope without qualification:

| Base class | Input type | Use it for |
| --- | --- | --- |
| `CharGrammar` | `char` | Lexers and other character layers |
| `TokenGrammar<TToken, TKind>` | `TToken`, which implements `IKinded<TKind>` | Parsers over tokens with kinds |
| `Grammar<TIn>` | `TIn` | Any other input type, such as `byte` |

```csharp
public sealed class Versions : CharGrammar
{
    public static readonly Parser<char, int> Default = Return(1);
    public static readonly Parser<char, int> Unsupported = Fail<int>("unsupported version");
}
```

- `Return(value)` succeeds without consuming input and produces `value`.
- `Fail<TOut>(message)` fails without consuming input and reports `message`. C# cannot infer `TOut` from the message, so pass it explicitly.
- Declare each rule once, in a `static readonly` field. Grammar classes are not instantiated.
- The combinators are public, so code outside a grammar class can call them qualified, for example `Grammar<char>.Return(1)`.

The static `Grammar` class holds operations on whole grammars. `Grammar.Deterministic(p)` will check that `p` is LL(1). The check is not implemented yet, so for now it returns `p` unchanged.

## Recursion with `Rec`

Field initializers run in declaration order. A rule that refers to a rule declared after it, or to itself, would read a field that is still null. `Rec` defers the reference until the grammar is first used:

```csharp
public sealed class Lists : CharGrammar
{
    // List is declared below, so this reference needs Rec.
    private static readonly Parser<char, int> Item = Rec(() => List);

    // A list is "[", then lists separated by ",", then "]". It produces how many lists it contains.
    public static readonly Parser<char, int> List =
        Item.SepBy(Char(',')).Between(Char('['), Char(']')).Select(static items => items.Count);
}
```

- `Rec(() => p)` calls the lambda once, when the grammar is first used, not when `Rec` is called. Every use then sees the same parser.
- Wrap a reference in `Rec` when the field it reads is declared later, or when it closes a cycle between rules. Other references need no `Rec`.
- If the lambda returns null at first use, Creole throws a `GrammarException` naming the lambda.
- The lambda's return type is nullable, so `Rec(() => LaterField)` compiles without a nullable warning. A later field used *inside* a larger lambda, such as `Rec(() => OneOf(A, LaterField))`, still gets warning CS8604, because the compiler does not know the lambda runs after the field is set. Write `LaterField!`, or wrap that reference in its own `Rec`.

### Rule names

`Rec` also names the rule, for diagnostics such as `GrammarException` rule paths:

| You write | The rule's name |
| --- | --- |
| `Rec(() => Value)` | `Value`, the member the lambda returns |
| `Rec(() => JsonGrammar.Value)` | `JsonGrammar.Value` |
| `Value = Rec(() => OneOf(...))` | `Value`, the field being initialized |

The rules are in [ADR-0009](../../work/docs/decisions/0009-where-rule-names-come-from.md). Rules that do not go through `Rec` have no name yet; the source generator will name every rule field of a `partial` grammar class.

## Tokens with kinds

A token type can implement `IKinded<TKind>` to expose an enum kind. Make tokens structs so they never box:

```csharp
public enum J { String, Number, LeftBrace, RightBrace }

public readonly record struct JTok(J Kind, ReadOnlyMemory<char> Text) : IKinded<J>;
```

## `GrammarException`

Building an invalid grammar throws a `GrammarException`, whose message says what is wrong.

Combinators reject a null parser argument with a `GrammarException` that names the expression you passed. A null parser is usually a field whose initializer has not run yet, because it is declared later in the class. The message suggests the fix:

```text
'document' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => document).
```
