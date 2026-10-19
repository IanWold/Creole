namespace Creole.Tests;

public class ErrorModelTests
{
    private static readonly SourceSpan _at3 = new(SourceId.Unnamed, 3, 4);

    private static string DescribeFound(Unexpected unexpected) => unexpected switch
    {
        UnexpectedItem item => item.Display,
        EndOfInput => "end of input",
        CustomMessage message => message.Text,
    };

    private static string DescribeWanted(Expectation expectation) => expectation switch
    {
        Label label => label.Name,
        ExpectedItem item => item.Display,
        EndOfInput => "end of input",
    };

    [Fact]
    public void EndOfInputIsEitherUnion()
    {
        var end = new EndOfInput();

        Unexpected unexpected = end;
        Expectation expectation = end;

        Assert.Same(end, unexpected.Value);
        Assert.Same(end, expectation.Value);
    }

    [Fact]
    public void ErrorsCopyTheirLists()
    {
        List<Expectation> expected = [new Label("value")];
        List<string> context = ["in array"];

        var error = new ParseError(_at3, "parser", new EndOfInput(), expected, context);
        var rendered = error.ToString();
        var hash = error.GetHashCode();

        expected.Add(new EndOfInput());
        context.Clear();

        Assert.Equal(rendered, error.ToString());
        Assert.Equal(hash, error.GetHashCode());
        Assert.True(((ICollection<Expectation>)error.Expected).IsReadOnly);
        Assert.True(((ICollection<string>)error.Context).IsReadOnly);
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void ErrorsDifferingInAnyPartAreNotEqual(ParseError variant)
    {
        var baseline = new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [new Label("value")], []);

        Assert.NotEqual(baseline, variant);
    }

    [Fact]
    public void ErrorsWithEqualPartsAreEqual()
    {
        var a = new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [new Label("value"), new EndOfInput()], ["in array"]);
        var b = new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [new Label("value"), new EndOfInput()], ["in array"]);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ExpectationOrderMatters()
    {
        var a = new ParseError(_at3, "parser", new EndOfInput(), [new ExpectedItem("','"), new ExpectedItem("'}'")], []);
        var b = new ParseError(_at3, "parser", new EndOfInput(), [new ExpectedItem("'}'"), new ExpectedItem("','")], []);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ExpectationSwitchCoversEveryCase()
    {
        Assert.Equal("value", DescribeWanted(new Label("value")));
        Assert.Equal("','", DescribeWanted(new ExpectedItem("','")));
        Assert.Equal("end of input", DescribeWanted(new EndOfInput()));
    }

    [Fact]
    public void NullPartsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new ParseError(_at3, null!, new EndOfInput(), [], []));
        Assert.Throws<ArgumentNullException>(() => new ParseError(_at3, "parser", new EndOfInput(), null!, []));
        Assert.Throws<ArgumentNullException>(() => new ParseError(_at3, "parser", new EndOfInput(), [], null!));
        Assert.Throws<ArgumentException>(() => new ParseError(_at3, "parser", new EndOfInput(), [], [null!]));
    }

    [Fact]
    public void ParseErrorKeepsItsParts()
    {
        var error = new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [new ExpectedItem("','")], ["in array"]);

        Assert.Equal(_at3, error.Span);
        Assert.Equal("parser", error.Layer);
        Assert.Equal(new UnexpectedItem("'x'"), error.Unexpected.Value);
        Assert.Equal(new ExpectedItem("','"), Assert.Single(error.Expected).Value);
        Assert.Equal("in array", Assert.Single(error.Context));
    }

    [Fact]
    public void UnexpectedSwitchCoversEveryCase()
    {
        Assert.Equal("string \"host\"", DescribeFound(new UnexpectedItem("string \"host\"")));
        Assert.Equal("end of input", DescribeFound(new EndOfInput()));
        Assert.Equal("number too large", DescribeFound(new CustomMessage("number too large")));
    }

    [Fact]
    public void UnionsWithNoCaseAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new ParseError(_at3, "parser", default, [], []));
        Assert.Throws<ArgumentException>(() => new ParseError(_at3, "parser", new EndOfInput(), [default], []));
    }

    public static TheoryData<ParseError> Variants() =>
    [
        new ParseError(new SourceSpan(SourceId.Unnamed, 4, 5), "parser", new UnexpectedItem("'x'"), [new Label("value")], []),
        new ParseError(_at3, "lexer", new UnexpectedItem("'x'"), [new Label("value")], []),
        new ParseError(_at3, "parser", new EndOfInput(), [new Label("value")], []),
        new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [new Label("number")], []),
        new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [new Label("value"), new EndOfInput()], []),
        new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [], []),
        new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [new Label("value")], ["in array"]),
    ];

    [Fact]
    public void WithChangesEachPart()
    {
        var error = new ParseError(_at3, "parser", new EndOfInput(), [new Label("value")], ["in array"]);

        Assert.Equal(
            new ParseError(_at3, "lexer", new UnexpectedItem("'x'"), [new ExpectedItem("','")], ["in object"]),
            error with { Layer = "lexer", Unexpected = new UnexpectedItem("'x'"), Expected = [new ExpectedItem("','")], Context = ["in object"] }
        );
    }

    [Fact]
    public void WithChecksAndCopiesLikeTheConstructor()
    {
        var error = new ParseError(_at3, "parser", new EndOfInput(), [], []);
        List<Expectation> expected = [new Label("value")];

        var changed = error with { Expected = expected };
        expected.Add(new EndOfInput());

        Assert.Single(changed.Expected);
        Assert.True(((ICollection<Expectation>)changed.Expected).IsReadOnly);
        Assert.Throws<ArgumentNullException>(() => error with { Layer = null! });
        Assert.Throws<ArgumentException>(() => error with { Unexpected = default });
        Assert.Throws<ArgumentException>(() => error with { Expected = [default] });
        Assert.Throws<ArgumentNullException>(() => error with { Context = null! });
    }
}
