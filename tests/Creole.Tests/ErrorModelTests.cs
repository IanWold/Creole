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
    public void UnexpectedSwitchCoversEveryCase()
    {
        Assert.Equal("string \"host\"", DescribeFound(new UnexpectedItem("string \"host\"")));
        Assert.Equal("end of input", DescribeFound(new EndOfInput()));
        Assert.Equal("number too large", DescribeFound(new CustomMessage("number too large")));
    }

    [Fact]
    public void ExpectationSwitchCoversEveryCase()
    {
        Assert.Equal("value", DescribeWanted(new Label("value")));
        Assert.Equal("','", DescribeWanted(new ExpectedItem("','")));
        Assert.Equal("end of input", DescribeWanted(new EndOfInput()));
    }

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
    public void ErrorsWithEqualPartsAreEqual()
    {
        var a = new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [new Label("value"), new EndOfInput()], ["in array"]);
        var b = new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [new Label("value"), new EndOfInput()], ["in array"]);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
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

    [Theory]
    [MemberData(nameof(Variants))]
    public void ErrorsDifferingInAnyPartAreNotEqual(ParseError variant)
    {
        var baseline = new ParseError(_at3, "parser", new UnexpectedItem("'x'"), [new Label("value")], []);

        Assert.NotEqual(baseline, variant);
    }

    [Fact]
    public void ExpectationOrderMatters()
    {
        var a = new ParseError(_at3, "parser", new EndOfInput(), [new ExpectedItem("','"), new ExpectedItem("'}'")], []);
        var b = new ParseError(_at3, "parser", new EndOfInput(), [new ExpectedItem("'}'"), new ExpectedItem("','")], []);

        Assert.NotEqual(a, b);
    }
}
