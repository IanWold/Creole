using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file enum Lexeme
{
    Comma,
    Word,
    Blank,
}

file readonly record struct Described(Lexeme Kind, string Text) : IKinded<Lexeme>
{
    static string IKinded<Lexeme>.Describe(Lexeme kind) => kind switch
    {
        Lexeme.Comma => "','",
        Lexeme.Word => "word",
        _ => "",
    };

    string? IKinded<Lexeme>.Detail =>
        Kind == Lexeme.Word
        ? Text
        : null;
}

file readonly record struct Plain(Lexeme Kind) : IKinded<Lexeme>;

file sealed record Name(string Value);

file sealed record Comma;

file union Piece(Name, Comma) : IKinded<Lexeme>
{
    public readonly Lexeme Kind =>
        Value is Comma
        ? Lexeme.Comma
        : Lexeme.Word;
}

file sealed class NeverScanner(ScannerInfo info) : IScanner<char, Unit>
{
    public ScannerInfo Info { get; } = info;

    public bool Scan(ref Cursor<char> cursor, out Unit value)
    {
        value = default;
        return false;
    }
}

file static class Tokens
{
    public static readonly Parser<Described, Described> Comma = TokenGrammar<Described, Lexeme>.Kind(Lexeme.Comma);

    public static readonly Parser<Described, Described> Blank = TokenGrammar<Described, Lexeme>.Kind(Lexeme.Blank);

    public static readonly Parser<Plain, Plain> PlainComma = TokenGrammar<Plain, Lexeme>.Kind(Lexeme.Comma);

    public static readonly Parser<Piece, Name> Name = TokenGrammar<Piece, Lexeme>.Case<Name>();
}

public class FailureRecordingTests
{
    private static ParseError At(long start, long end, Unexpected unexpected, params Expectation[] expected) =>
        new(new SourceSpan(SourceId.Unnamed, start, end), "parser", unexpected, expected, []);

    [Fact]
    public void ACharacterExpectsItself() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'b'"), new ExpectedItem("'a'")), Execution.Error(Char('a'), "b"));

    [Fact]
    public void AFailureAtTheEndOfInputFindsTheEnd() =>
        Assert.Equal(At(0, 0, new EndOfInput(), new ExpectedItem("'a'")), Execution.Error(Char('a'), ""));

    [Fact]
    public void ANamedClassExpectsItsName() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'x'"), new Label("digit")), Execution.Error(Digit, "x"));

    [Fact]
    public void AnUnnamedClassIsOneExpectation()
    {
        Assert.Equal(At(0, 1, new UnexpectedItem("'1'"), new ExpectedItem("[a-z]")), Execution.Error(Range('a', 'z'), "1"));
        Assert.Equal(At(0, 1, new UnexpectedItem("'x'"), new ExpectedItem("any character except 'x'")), Execution.Error(NoneOf('x'), "x"));
        Assert.Equal(
            At(0, 1, new UnexpectedItem("'\"'"), new ExpectedItem($"any character except {CharSet.Of('"', '\\')}")),
            Execution.Error(NoneOf('"', '\\'), "\"")
        );
    }

    [Fact]
    public void AStringExpectsItselfWhereItStarted()
    {
        Assert.Equal(At(0, 1, new UnexpectedItem("'l'"), new ExpectedItem("\"let\"")), Execution.Error(String("let"), "lex"));
        Assert.Equal(At(0, 1, new UnexpectedItem("'S'"), new ExpectedItem("\"select\"")), Execution.Error(CIString("select"), "SET"));
    }

    [Fact]
    public void EndExpectsTheEndOfInput() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'x'"), new EndOfInput()), Execution.Error(End, "x"));

    [Fact]
    public void AnyAndSatisfyExpectNothingInParticular()
    {
        Assert.Equal(At(0, 0, new EndOfInput()), Execution.Error(Any, ""));
        Assert.Equal(At(0, 1, new UnexpectedItem("'a'")), Execution.Error(Satisfy(char.IsUpper), "a"));
    }

    [Fact]
    public void FailReportsItsMessage() =>
        Assert.Equal(At(0, 1, new CustomMessage("no value here")), Execution.Error(Fail<int>("no value here"), "x"));

    [Fact]
    public void ATokenExpectsItsValue() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("4"), new ExpectedItem("3")), Execution.Error(Grammar<int>.Token(3), [4]));

    [Fact]
    public void KindsAndTokensShowAsTheTokenTypeDescribesThem()
    {
        Assert.Equal(At(0, 1, new UnexpectedItem("word host"), new ExpectedItem("','")), Execution.Error(Tokens.Comma, [new Described(Lexeme.Word, "host")]));
        Assert.Equal(At(0, 1, new UnexpectedItem("','"), new ExpectedItem("Blank")), Execution.Error(Tokens.Blank, [new Described(Lexeme.Comma, ",")]));
        Assert.Equal(At(0, 1, new UnexpectedItem("Word"), new ExpectedItem("Comma")), Execution.Error(Tokens.PlainComma, [new Plain(Lexeme.Word)]));
    }

    [Fact]
    public void ACaseExpectsItsTypeName() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("Comma"), new ExpectedItem("Name")), Execution.Error(Tokens.Name, [new Piece(new Comma())]));

    [Fact]
    public void AScannerExpectsItsNameOrElseItsFirstCharacters()
    {
        Assert.Equal(At(0, 1, new UnexpectedItem("'x'"), new Label("raw string")), Execution.Error(Custom(new NeverScanner(new(false, CharSet.Of('r'), "raw string"))), "x"));
        Assert.Equal(At(0, 1, new UnexpectedItem("'x'"), new ExpectedItem("'r'")), Execution.Error(Custom(new NeverScanner(new(false, CharSet.Of('r')))), "x"));
        Assert.Equal(At(0, 1, new UnexpectedItem("'x'")), Execution.Error(Custom(new NeverScanner(new(false))), "x"));
    }

    [Fact]
    public void AMatchWithNoCaseExpectsNothingWhereItsBranchWouldStart()
    {
        var match = Any.Select(static c => (Piece)new Name(c.ToString())).Match(Case(Return(0), static (Comma _, int n) => n));

        Assert.Equal(At(1, 2, new UnexpectedItem("'b'")), Execution.Error(match, "ab"));
    }

    [Fact]
    public void FailuresAtOnePositionMergeInTheOrderRecorded() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'c'"), new ExpectedItem("'a'"), new ExpectedItem("'b'")), Execution.Error(Char('a').Or(Char('b')), "c"));

    [Fact]
    public void AnExpectationIsListedOnce()
    {
        var a = Char('a');

        Assert.Equal(At(0, 1, new UnexpectedItem("'c'"), new ExpectedItem("'a'")), Execution.Error(a.Or(a), "c"));
        Assert.Equal(At(0, 1, new UnexpectedItem("'c'"), new ExpectedItem("','")), Execution.Error(Char(',').Or(Char(',')), "c"));
    }

    [Fact]
    public void TheFurthestFailureWinsEvenPastAnAtomic() =>
        Assert.Equal(At(1, 2, new UnexpectedItem("'x'"), new ExpectedItem("'b'")), Execution.Error(Atomic(Char('a').Then(Char('b'))).Or(Char('c')), "ax"));

    [Fact]
    public void TheFirstMessageAtAPositionWinsAndKeepsTheExpectations() =>
        Assert.Equal(
            At(0, 1, new CustomMessage("first"), new ExpectedItem("'x'")),
            Execution.Error(OneOf(Fail<char>("first"), Fail<char>("second"), Char('x')), "y")
        );

    [Fact]
    public void AFurtherFailureDropsAMessage() =>
        Assert.Equal(At(1, 2, new UnexpectedItem("'c'"), new ExpectedItem("'b'")), Execution.Error(Fail<char>("m").Or(Char('a').Then(Char('b'))), "ac"));

    [Fact]
    public void ASuccessfulParseKeepsWhereItsLoopStopped() =>
        Assert.Equal(At(2, 2, new EndOfInput(), new Label("digit")), Execution.Error(Digit.Many(), "12"));

    [Fact]
    public void NothingFailingMeansNoError() =>
        Assert.Null(Execution.Error(Return(1), "x"));

    [Theory]
    [InlineData("12x")]
    [InlineData("")]
    [InlineData("12")]
    public void AScannedRunRecordsWhereItStopsAsTheLoopDoes(string input)
    {
        var parser = Digit.SkipMany().Then(Char(';'));

        Assert.Equal(Execution.Error(parser, input), Execution.Error(parser, input, LoweringOptions.Scan));
    }

    [Fact]
    public void AScannedRunThatReachesItsMaximumRecordsNothingThere()
    {
        var parser = new SkipRepeatNode<char, char>(Digit, 0, 2).Then(Char(';'));

        Assert.Equal(At(2, 3, new UnexpectedItem("'3'"), new ExpectedItem("';'")), Execution.Error(parser, "123", LoweringOptions.Scan));
        Assert.Equal(Execution.Error(parser, "123"), Execution.Error(parser, "123", LoweringOptions.Scan));
    }

    [Fact]
    public void ALogCanBeCleared()
    {
        var log = new FailureLog<char>();

        log.Record(3, "m");
        log.Clear();

        Assert.Equal(-1, log.Position);
    }

    [Theory]
    [InlineData('a', "'a'")]
    [InlineData('\'', @"'\''")]
    [InlineData('\\', @"'\\'")]
    [InlineData('\n', @"'\n'")]
    [InlineData('\0', @"'\0'")]
    [InlineData('\u001B', @"'\u001B'")]
    [InlineData('\u200D', @"'\u200D'")]
    [InlineData('\uD800', @"'\uD800'")]
    [InlineData('é', "'é'")]
    [InlineData('"', "'\"'")]
    public void CharactersShowQuotedWithHiddenOnesEscaped(char c, string shown) =>
        Assert.Equal(shown, Display.Character(c));

    [Fact]
    public void StringsShowQuotedWithHiddenCharactersEscaped()
    {
        Assert.Equal(@"""a\""b\n'""", Display.Text("a\"b\n'"));
        Assert.Equal("\"\U0001F44D\"", Display.Text("\U0001F44D"));
        Assert.Equal(@"""\uD800x""", Display.Text("\uD800x"));
    }

    [Fact]
    public void TypeNamesReadAsWritten()
    {
        Assert.Equal("Name", Display.TypeName(typeof(Name)));
        Assert.Equal("List", Display.TypeName(typeof(List<int>)));
        Assert.Equal("String", Display.TypeName(typeof(string)));
    }

    [Fact]
    public void EveryCharacterIsAnyCharacter() =>
        Assert.Equal("any character", Display.CharacterSet(CharSet.All));
}
