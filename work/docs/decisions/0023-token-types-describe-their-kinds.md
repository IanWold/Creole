# ADR-0023: Token types describe their kinds through a static virtual member of `IKinded`, and an unexpected token adds its own detail

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-25 |
| Cards | [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), [CORE-02](../../active/02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md), [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md) |
| Whitepaper | [§ Types](../../../whitepaper.md#types), [§ The error model](../../../whitepaper.md#the-error-model), [§ A. JSON in two layers](../../../whitepaper.md#a-json-in-two-layers) |
| Research | — (a spike on the pinned SDK and prior art read on 2026-10-25, both described below) |
| Supersedes | — |

## Context

[§ The error model](../../../whitepaper.md#the-error-model) says "`TokenGrammar` has an overridable `Describe(TKind)`, so expectations read `','` or `'}'` rather than `Comma` or `RBrace`". Its rendered example also shows an unexpected token with its text: `unexpected string "host"`.

[CORE-02](../../active/02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md) moved `Describe` to ERR-02 unbuilt, for two reasons:

- Grammar classes hold static fields and are never instantiated, so an instance `virtual` has no caller.
- A class cannot declare static virtual members.

[ADR-0021](0021-how-failures-are-recorded-and-merged.md) point 6 records `Kind(k)` as the kind, and formats display strings when the error is built. It leaves the kind's display to this record.

Two things need a display:

- **An expected kind**, such as `','` for `J.Comma`.
- **An unexpected token**, such as `string "host"`. That needs the kind's name and something from the token itself, which `IKinded<TKind>` does not expose today.

**A spike on the pinned SDK** (11.0.100-rc.2.26504.105), in a scratch project outside the repository. It used:

- an interface with `static virtual string Describe(TKind kind) => kind.ToString()` and an instance member `string? Detail => null`;
- a `readonly record struct` token, and a C# 15 `union` token, each overriding `Describe` with an explicit implementation (`static string IKinded<J>.Describe(J kind) => …`);
- a token that overrides nothing;
- a generic class constrained to `TToken : IKinded<TKind>` that calls `TToken.Describe(kind)` and `token.Detail`.

It found:

- **Overrides.** The struct and union overrides were used, and the token that overrides nothing got the defaults.
- **Type arguments.** `List<IKinded<J>>` compiles. Every static member has a default, so the interface can still be a type argument.
- **Output.** The generic code produced `','`, `'}'` and `string "host"`.
- **Native AOT.** `dotnet publish` with `PublishAot` and warnings as errors gave no warnings and the same output, including the `kind.ToString()` default and `typeof(T).Name`.

Prior art, read on 2026-10-25 on each project's `dev` branch:

- **Superpower** ([`TokenAttribute.cs`](https://github.com/datalust/superpower/blob/dev/src/Superpower/Display/TokenAttribute.cs), [`Presentation.cs`](https://github.com/datalust/superpower/blob/dev/src/Superpower/Display/Presentation.cs)). An attribute on enum fields, `[Token(Category, Example, Description)]`, is read by reflection, with no caching in that file.
  - An expected kind shows `Description`, else `Example` in backticks, else the kind's name in lower case.
  - An unexpected token shows its text clipped to 12 characters and quoted, after `Category` or the kind's name when one applies.
- **ANTLR** ([`VocabularyImpl.java`](https://github.com/antlr/antlr4/blob/dev/runtime/Java/src/org/antlr/v4/runtime/VocabularyImpl.java)). `getDisplayName` prefers an explicit display name, then the literal name, then the symbolic name, then the number.

Both show expected kinds by name, and unexpected tokens with their text.

Constraints:

- **G8.** AOT and trim safe, and IL2xxx and IL3xxx warnings are errors.
- **G7.** Both engines must produce the same strings.
- **G10.** Simple to write.
- **Pre-1.0 churn.** Prefer additive changes to the Done CORE-02 surface.

## Options

### Where kind descriptions live

#### A. A static virtual member of `IKinded<TKind>` (chosen)

```csharp
public interface IKinded<TKind> where TKind : struct, Enum
{
    TKind Kind { get; }
    static virtual string Describe(TKind kind) => kind.ToString();
}
```

The token type overrides it, and engines call `TToken.Describe(kind)`. `TokenGrammar` already constrains `TToken : IKinded<TKind>`, so every `Kind` node and generated parser can make that call.

**Pros:**

- Additive: existing tokens compile unchanged.
- AOT-clean, as spiked.
- The names live with the token vocabulary, which a lexer and its parsers share, so every grammar over a token type uses the same names.
- `Detail` sits on the same interface.

**Cons:**

- The override uses explicit interface syntax, which many C# users have not seen.
- A token type the author does not own cannot be described. Wrap it, or use `Named`.

#### B. A self-typed grammar

```csharp
public sealed partial class JsonGrammar : TokenGrammar<JsonGrammar, JTok, J>, IDescribeKinds<J>
{
    public static string Describe(J kind) => …;
}
```

**Pros:**

- A plain static method next to the rules.

**Cons:**

- Breaking: every token grammar gains a type parameter, which changes every sample, the generator and the analyzers.
- CRTP is unfamiliar to many C# users.
- Grammars that share a token type each repeat the names. When rules from two classes mix, a message's names depend on which class built each `Kind` node.

#### C. A describer passed when parsing

For example, `ParseOptions.Describe` or a parameter of the pipeline.

**Pros:**

- Localization can differ for each call.

**Cons:**

- Analysis, Explain and EBNF export never see the names.
- Every call site must pass the describer.
- The names sit apart from the token type.

#### D. Attributes on enum members, as Superpower does

**Pros:**

- The most declarative to write: `[Token(Example = ",")] Comma`.

**Cons:**

- Reading attributes off a generic `TKind` raises trim warnings. Silencing them needs `[DynamicallyAccessedMembers(PublicFields)]` on `TKind` in every public generic that carries it.
- The alternative is a table the generator emits, which leaves interpreter-only grammars on reflection.
- The token's text still needs a member on the token.

#### E. No customization

Kinds show by name, and `Kind(J.Comma).Named("','")` renames one use.

**Cons:**

- Labels on every punctuation token.
- The whitepaper's message would read "expected Comma or RBrace".

### How an unexpected token is shown

- **a. The kind's description plus the token's detail (chosen).** `IKinded<TKind>` gains `string? Detail => null`. The message shows `Describe(kind)`, followed by a space and the detail when there is one. A `JTok` returns its text for strings and numbers, so the message reads `string "host"`, and `','` stays `','`. This matches what ANTLR and Superpower show.
- **b. The kind's description only.** This reads "unexpected string". The carets still mark the text, but `Render()` without sources never shows it.
- **c. The token describes itself whole.** The default would be `string? Describe() => null`, and the token returns the whole phrase, repeating the kind's name where it wants one.

### When `Describe` runs

- **When the error is built (chosen).** A display string never affects parsing, so it has no reason to be frozen. This allows localization through the UI culture at report time, and costs nothing while parsing.
- **Once, when the `Kind` node is built**, as `Custom` reads `Info` ([ADR-0012](0012-custom-scanner-metadata.md) point 2). This is cheaper for tools, but it freezes the UI culture of the static initializer that builds the grammar.

## Decision

Option **A** with **a**, chosen by the maintainer in conversation on 2026-10-25. The maintainer accepted the record on the same day, with the timing as recommended.

1. **API.** `IKinded<TKind>` becomes:

   ```csharp
   public interface IKinded<TKind> where TKind : struct, Enum
   {
       TKind Kind { get; }

       static virtual string Describe(TKind kind) => kind.ToString();

       string? Detail => null;
   }
   ```

   `TokenGrammar` gains nothing, and the whitepaper's "overridable `Describe(TKind)` on `TokenGrammar`" moves here.

2. **Expected kinds.** `Kind(k)` records the kind (ADR-0021 point 6). Its `ExpectedItem` display is `TToken.Describe(k)`.

3. **Unexpected tokens.** For a token that implements `IKinded<TKind>`, the `UnexpectedItem` display is `TToken.Describe(token.Kind)`. When `token.Detail` is neither null nor empty, a space and the detail follow.

   Engines reach both members through a token grammar's type parameters, because AOT rules out finding them from the token type alone. So a token type shows this way once a `TokenGrammar<TToken, TKind>` over it has been initialised, which any grammar that uses `Kind` does. Until then it shows as point 5 says. A token type that implements `IKinded` for two kind enums shows through the token grammar over it that was initialised first.

4. **Timing.**
   - Engines call `Describe` and `Detail` only when a `ParseError` is built, never while parsing.
   - A null or empty `Describe` result is replaced by `kind.ToString()`, so building an error never fails on it. An exception from either member propagates.
   - Tools (Explain, EBNF export, CRL0501) call `Describe` the same way, through the `Kind` node's type parameters.

5. **Other items.** These are pinned for G7, and their formatting is a build detail, pinned by snapshot tests:
   - A character shows quoted, as `'x'`, with escapes for quotes, backslashes and control characters. `String(s)` and `CIString(s)` show as `"s"`.
   - `Token(x)`, and any item that is not a character and does not show as point 3 says, shows as `x.ToString()`.
   - `Case<TCase>()` records an expectation shown as `typeof(TCase).Name`. This fills a gap in ADR-0021 point 6's table. `Named` relabels it as usual.

Deciding reason: A is the only option that is additive, AOT-clean without annotations, and shared by every grammar over a token type. It also puts `Describe` and `Detail` in one place.

What would change this:

- Authors finding the explicit interface syntax a real obstacle. Then the generator could emit the `IKinded` implementation from attributes on the enum. That is D as sugar over A, with no reflection, and it would be added without breaking anything.
- A need to describe kinds differently per grammar over one token type. Then C, or a per-grammar override, would be added.

## Consequences

**Easier:**

- The whitepaper's rendered JSON error needs only the token's two members, with no labels.
- Analysis and tools reach the same names as the engines.
- Error text can be localized at report time.

**Harder:**

- Docs must show the explicit implementation syntax, with a whole token example.
- A `Describe` that throws turns a parse error into an exception. Docs say it must not throw.
- A struct token that keeps the default `Detail` is boxed for the call. That happens only when an error is built.

**Ruled out:**

- `Describe` on `TokenGrammar`.
- Reflection over kind enums.
- Descriptions frozen at grammar construction.

**Risks and how they are watched:**

- **The RC 2 behaviour of static virtual members in a union changing at GA.** [FND-09](../../active/01-foundations/FND-09-move-to-the-net-11-ga-sdk.md) re-runs the tests that rely on unions. ERR-02's tests include a union token that overrides `Describe`.
- **AOT.** `Creole.AotSmoke` gains a token that overrides both members.

## Changes made

- **Whitepaper:**
  - [§ Types](../../../whitepaper.md#types): the `IKinded` sketch gains `Describe` and `Detail`.
  - [§ The error model](../../../whitepaper.md#the-error-model): the **Display** bullet says that token types describe their kinds and add a detail to unexpected tokens, and that both run when an error is built.
  - [§ A. JSON in two layers](../../../whitepaper.md#a-json-in-two-layers): `JTok` gains a body with `Describe` and `Detail`. `ObjectValue` gains `.Context("in object")` (editorial), so the example produces the rendered error in § The error model.
- **Cards:**
  - [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md): the `Describe` scope item cites this record, and adds `Detail`, the display of `Case` and other items, and an AOT smoke token. ERR-02 moves to Building.
  - [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md): generated parsers call the same members when they build an error.
- **Decisions index:** a note that this record moves `Describe` from `TokenGrammar` to `IKinded`, and fills ADR-0021 point 6's `Kind` display and its missing `Case` row.
- **Edited in place during ERR-02's build (2026-10-25), before merge.** ERR-02's completion review found that tokens show through `Describe` only once a token grammar over them has been initialised. The maintainer chose to state that rule rather than look for another mechanism. Point 3 now says how engines reach the members, and point 5 refers to it. Whitepaper [§ The error model](../../../whitepaper.md#the-error-model), ERR-02's scope, [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md) and docs/concepts/primitives.md changed to match.
