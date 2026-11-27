# Mode channels

Some tokens can be told apart only by what the parser knows. In JavaScript-like syntax, `/` after `)` starts a regex in `if (ok) /x/.test(s)` but divides in `(a) / 2`. A mode channel carries that knowledge: the parser changes the channel's mode as it parses, and the lexer below it, a `Layer.Modal`, reads each token in the mode current when it gets there. The design is in the [whitepaper's § 3. Mode channels](../../whitepaper.md#3-mode-channels) and [ADR-0041](../../work/docs/decisions/0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md).

## A channel

A `ModeChannel<TMode>` is a typed key with an initial mode. A channel is data, as a parser is: it holds its initial mode and nothing else. Each parse keeps its own stack of modes for each channel, which starts as the initial mode alone, so one channel serves any number of parses at once, on any thread.

A channel is known by its instance, not by its type or its initial mode. Two `new ModeChannel<LexMode>(LexMode.Operand)` are two channels. Keep each channel in a `static readonly` field and use that field everywhere.

## Changing the mode

`SetMode`, `PushMode` and `PopMode` are members of `Grammar<TIn>`, so a grammar class calls them unqualified. Each consumes nothing and produces `Unit`, so it composes like any other parser:

| Combinator | Does |
| --- | --- |
| `SetMode(channel, mode)` | Replaces the top of the stack with `mode`. The stack keeps its depth. |
| `PushMode(channel, mode)` | Pushes `mode`, which is current until a `PopMode` returns to the mode below it. |
| `PopMode(channel)` | Pops the top of the stack. |

The current mode is the top of the stack. `SetMode` suits modes that follow one another, such as operand and operator. `PushMode` and `PopMode` suit modes that nest, such as a block:

```csharp
public enum LexMode { Operand, Operator, Block }

public sealed class Script : TokenGrammar<Tok, K>
{
    public static readonly ModeChannel<LexMode> Modes = new(LexMode.Operand);

    public static readonly Parser<Tok, Tok> Operand =
        Kind(K.Number).Between(SetMode(Modes, LexMode.Operand), SetMode(Modes, LexMode.Operator));

    public static readonly Parser<Tok, Unit> Block =
        Kind(K.LBrace).Then(PushMode(Modes, LexMode.Block)).Then(Operand.SkipMany()).Before(Kind(K.RBrace)).Before(PopMode(Modes));
}
```

Pair each `PopMode` with a `PushMode` through the grammar's structure, as `Block` does. A path that pops more than it pushed is a mistake in the grammar, not in the input, so popping the last mode throws `GrammarException` rather than failing the parse:

```text
The parser layer pops the last mode of a ModeChannel<LexMode>. Each PopMode needs a PushMode on the same channel before it.
```

## Reading a channel: `Layer.Modal`

`Layer.Modal(channel, selector, skip)` is a lexer that reads the channel's current mode at each step and runs the item parser `selector` gives for it ([Layers](layers.md#layermodal)). Here the parser decides, before each `/`, whether it starts a regex or divides:

```csharp
public enum LexMode { Operand, Operator }
public enum K { Name, Slash, Regex }
public readonly record struct Lexeme(K Kind, string Text) : IKinded<K>;

public sealed class ScriptLexer : CharGrammar
{
    public static readonly ModeChannel<LexMode> Modes = new(LexMode.Operand);

    private static readonly Parser<char, Lexeme> _name = Letter.AtLeastOnce().Text().Select(static text => new Lexeme(K.Name, text));
    private static readonly Parser<char, Lexeme> _regex = Char('/').Then(NoneOf('/').SkipMany()).Then(Char('/')).Text().Select(static text => new Lexeme(K.Regex, text));
    private static readonly Parser<char, Lexeme> _slash = Char('/').As(new Lexeme(K.Slash, "/"));

    public static readonly Layer<char, Lexeme> Lexer = Layer.Modal(Modes, static mode => mode switch
    {
        LexMode.Operand => _name.Or(_regex),
        _ => _name.Or(_slash),
    }, Whitespace.SkipMany());
}

public sealed class ScriptGrammar : TokenGrammar<Lexeme, K>
{
    public static readonly Parser<Lexeme, string> Division =
        Seq(Kind(K.Name), SetMode(ScriptLexer.Modes, LexMode.Operator), Kind(K.Slash), SetMode(ScriptLexer.Modes, LexMode.Operand), Kind(K.Regex))
            .Select(static (name, _, slash, _, regex) => $"{name.Text} {slash.Text} {regex.Text}");
}
```

`Pipeline.From(ScriptLexer.Lexer).Then(ScriptGrammar.Division).Parse("a / /b/")` produces `a / /b/`: the first `/` is lexed in operator mode as a slash, and `/b/` in operand mode as a regex. A lexer left in operand mode would read `/ /` as one regex.

## Tokens read ahead

The parser often looks at the next token before it changes the mode, and the lexer reads that token in the mode current when the parser asks for it. When the parser then changes the mode, the `Layer.Modal` directly below discards every token it read from the parser's position on, and reads them again in the new mode when the parser asks for them. Its diagnostics from those tokens are withdrawn, because reading them again raises its own. Tokens the parser has already consumed stay as they were read ([whitepaper § 3. Mode channels](../../whitepaper.md#3-mode-channels), [ADR-0042](../../work/docs/decisions/0042-a-write-discards-only-when-it-changes-the-top-mode-and-the-mode-log-keeps-only-what-a-rewind-can-reach.md)).

In a JavaScript-like grammar such as the whitepaper's [Example C](../../whitepaper.md#c-a-parser-that-steers-its-lexer), a statement after `x;` can start with a choice that looks at its first token while the lexer is still in operator mode after `x`, so the `/` of `/re/` is read as a slash. The operand that follows sets operand mode, and the slash is read again as the regex `/re/`. Usually a change discards one token or none.

Two cases still need the mode changed before the parser looks:

- **A token the old mode cannot read.** If the lexer fails to read the token in the old mode, the parse ends there, before the parser can change the mode. In such a grammar, the `/ c;` of `a / b / c;` read in operand mode is an unterminated regex. So an operand sets operator mode before it looks for anything that follows it, such as a `.` that starts a call.
- **A layer in between.** A token is read again only when the parser reads the `Layer.Modal`'s tokens directly. If another layer sits between them, a change applies only to the tokens the lexer reads after it.

## Who reads and writes a channel

The channel connects a `Layer.Modal` to the layers above it ([ADR-0041](../../work/docs/decisions/0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md)):

- **One reader.** A chain has one `Layer.Modal` for each channel. `Then` throws `GrammarException` when a chain would hold two.
- **One writer.** The grammar of any layer above the `Layer.Modal`, or the parser that ends the pipeline, may change the mode, but only one of them in a run: the first to change it. A change from a second layer throws `GrammarException`.
- **Only above its reader.** In a pipeline, a change to a channel that no layer below the writer reads throws `GrammarException`. That catches a second `ModeChannel` instance where the lexer's was meant, and a layer that changes its own channel or one read above it:

```text
The parser layer changes the mode of a ModeChannel<LexMode>, but no layer below it reads that channel. Read it with Layer.Modal in a layer below, using the same ModeChannel instance.
```

## A parser on its own

A parser run on its own, with `Parse` over text or over a list of tokens, has no lexer to steer. It keeps its channels' stacks, so `PopMode` still checks its pairs, but nothing reads the modes. So a parser that steers its lexer can be tested over tokens that are already lexed: `Script.Block` parses `{ 1 2 }` given as the tokens `LBrace`, `Number`, `Number` and `RBrace`.

## Where each parse starts

Each parse starts every channel at its initial mode. Under [`ParseMany`](pipelines.md#parsemany), each item starts the channels the parser writes at their initial modes, so an item is lexed as it would be at the start of the input, and a token the last item read ahead in another mode is read again. A layer that writes a channel runs once for the whole input, so its changes last across items.

## What a change does to errors

A write changes the mode only when it leaves a different mode on top of the stack, compared with `EqualityComparer<TMode>.Default`. A `SetMode` to the current mode, a `PushMode` of it, and a `PopMode` that uncovers the same mode change nothing.

A change of mode forgets what the parse's furthest failure says about the input from the change on, because the lexer may read that input differently in the new mode ([whitepaper § Failure positions](../../whitepaper.md#failure-positions), [ADR-0042](../../work/docs/decisions/0042-a-write-discards-only-when-it-changes-the-top-mode-and-the-mode-log-keeps-only-what-a-rewind-can-reach.md)):

- A failure past the change becomes a failure where the change was made, which expects nothing. The next failure recorded there adds its expectations and its context.
- A rejected value or a chained operator whose input runs past the change no longer widens the error, and its message no longer takes the place of what was found.
- A failure that a construct puts back, as a filter does before it reports its rejection, is treated the same way for the changes made since it was saved.

So with `Modes` in operand mode, `Atomic(Char('a').Then(Char('b')).Then(Char('c'))).Or(SetMode(Modes, LexMode.Operator).Then(Char('z')))` on `abx` reports "expected 'z'" at the `a`, not "expected 'c'" at the `x`. With `SetMode(Modes, LexMode.Operand)`, which keeps the mode, the report stays at the `x`.

## Modes that carry data

A mode can carry data. Make the mode type a union whose cases are records, and the selector's `switch` covers every case with no `_` arm. The selector sees each mode value, data included, and builds that value's item parser ([ADR-0043](../../work/docs/decisions/0043-layer-modal-plans-each-mode-value-and-keeps-the-64-most-recent.md)).

First ask whether the lexer could decide the data itself. A heredoc's terminator is in the text the lexer reads, so the lexer can read the heredoc as one item with `Bind`, or keep the terminator in its [layer state](layer-state.md) when the body starts after other tokens. Data in a mode fits when it follows structure the parser already tracks. Here a block declares a word that is a keyword until the block ends. The parser knows where the block ends, and the lexer would have to match braces to know it:

```csharp
public enum K { Name, Keyword, LBrace, RBrace, Semicolon }
public readonly record struct Lexeme(K Kind, string Text) : IKinded<K>;

public sealed record Plain;
public sealed record Keyword(string Word);
public union LexMode(Plain, Keyword);

public sealed class BlockLexer : CharGrammar
{
    public static readonly ModeChannel<LexMode> Modes = new(new Plain());

    private static readonly Parser<char, Lexeme> _punctuation = OneOf(
        Char('{').As(new Lexeme(K.LBrace, "{")),
        Char('}').As(new Lexeme(K.RBrace, "}")),
        Char(';').As(new Lexeme(K.Semicolon, ";"))
    );

    private static readonly Parser<char, string> _word = Letter.AtLeastOnce().Text();

    public static readonly Layer<char, Lexeme> Lexer = Layer.Modal(Modes, static mode => mode switch
    {
        Plain => _word.Select(static text => new Lexeme(K.Name, text)).Or(_punctuation),
        Keyword keyword => _word.Select(text => new Lexeme(text == keyword.Word ? K.Keyword : K.Name, text)).Or(_punctuation),
    }, Whitespace.SkipMany());
}

public sealed class BlockGrammar : TokenGrammar<Lexeme, K>
{
    // keyword w makes w a keyword until the block ends.
    private static readonly Parser<Lexeme, Unit> _declaration =
        Kind(K.Name).Where(static name => name.Text == "keyword")
            .Then(Kind(K.Name))
            .Bind(static word => PushMode(BlockLexer.Modes, new Keyword(word.Text)));

    private static readonly Parser<Lexeme, string> _word = OneOf(Kind(K.Name), Kind(K.Keyword)).Select(static word => $"{word.Kind} {word.Text}");

    private static readonly Parser<Lexeme, string> _block =
        Kind(K.LBrace).Then(_declaration).Then(Kind(K.Semicolon)).Then(_word.Many())
            .Before(Kind(K.RBrace)).Before(PopMode(BlockLexer.Modes))
            .Select(static words => string.Join(", ", words));

    public static readonly Parser<Lexeme, string> Script = _block.Or(_word).Many().Before(End).Select(static parts => string.Join(", ", parts));
}
```

`Pipeline.From(BlockLexer.Lexer).Then(BlockGrammar.Script).Parse("foo { keyword foo; foo bar } foo")` produces `Name foo, Keyword foo, Name bar, Name foo`. Two blocks that declare different words each lex with their own: `{ keyword foo; foo bar } { keyword bar; foo bar }` produces `Keyword foo, Name bar, Name foo, Keyword bar`.

- **The parser writes data it has read.** The word comes from the input, so `PushMode(BlockLexer.Modes, new Keyword(word.Text))` sits inside `Bind`, which builds and plans the mode node at each declaration. A declaration is rare, so that costs little.
- **The layer keeps 64 plans.** The selector runs only for a mode whose plan the layer does not keep, and the layer keeps the plans of the 64 modes used most recently. A mode used again after its plan was dropped is planned again. A mode in steady use, such as `Plain` between blocks, stays planned while many keywords come and go.
- **Make the data immutable, with value equality.** Modes compare with `EqualityComparer<T>.Default`. Records compare by value, so two blocks that declare the same word share one plan. Data changed after it is written can match a plan made for its old value.
