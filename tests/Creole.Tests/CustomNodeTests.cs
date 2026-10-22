namespace Creole.Tests;

file static class Scanners
{
    public static readonly IScanner<char, int> Missing = null!;
}

file sealed class StubScanner<TIn>(ScannerInfo? declared) : IScanner<TIn, int>
{
    public ScannerInfo? Declared { get; set; } = declared;

    public ScannerInfo Info
    {
        get
        {
            InfoReads++;
            return Declared!;
        }
    }

    public int InfoReads { get; private set; }

    public bool Scan(ref Cursor<TIn> cursor, out int value)
    {
        value = 0;
        return false;
    }
}

public class CustomNodeTests
{
    private static CustomNode<TIn, int> Node<TIn>(Parser<TIn, int> parser)
    {
        INode node = parser;

        Assert.Equal(NodeKind.Custom, node.Kind);
        Assert.Empty(node.Children);

        return Assert.IsType<CustomNode<TIn, int>>(parser);
    }

    [Fact]
    public void ADeclaredFirstSetMakesTheNodeAnalyzable()
    {
        var scanner = new StubScanner<char>(new(Nullable: false, First: CharSet.Of('r'), Name: "raw string"));
        var node = Node(Grammar<char>.Custom(scanner));

        Assert.Same(scanner, node.Scanner);
        Assert.Equal(FirstSet.OfChars(CharSet.Of('r')), node.First);
        Assert.False(node.Nullable);
        Assert.False(node.Opaque);
        Assert.Equal("raw string", node.Name);
    }

    [Fact]
    public void ANullableDeclarationMakesTheNodeNullable() =>
        Assert.True(Node(Grammar<char>.Custom(new StubScanner<char>(new(Nullable: true, First: CharSet.Of('x'))))).Nullable);

    [Fact]
    public void CustomIsAnAlternativeInsideLongest()
    {
        var custom = Grammar<char>.Custom(new StubScanner<char>(new(Nullable: false, First: CharSet.Of('x'))));
        var node = Assert.IsType<LongestNode<char, int>>(Grammar<char>.Longest(custom, Grammar<char>.Return(0)));

        Assert.Same(custom, ((INode)node).Children[0]);
    }

    [Fact]
    public void InfoIsReadOnceWhenTheNodeIsBuilt()
    {
        var scanner = new StubScanner<char>(new(Nullable: false, First: CharSet.Of('a')));
        var node = Node(Grammar<char>.Custom(scanner));

        scanner.Declared = new(Nullable: true);

        Assert.Equal(1, scanner.InfoReads);
        Assert.False(node.Nullable);
        Assert.False(node.Opaque);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsAnEmptyName(string name)
    {
        var empty = new StubScanner<char>(new(Nullable: false, Name: name));

        Assert.Equal(
            "'empty' has an empty Name. Give the token a name for error messages, or leave Name null.",
            Assert.Throws<GrammarException>(() => Grammar<char>.Custom(empty)).Message
        );
    }

    [Fact]
    public void RejectsANullInfo()
    {
        var undeclared = new StubScanner<char>(null);

        Assert.Equal("The Info of 'undeclared' is null.", Assert.Throws<GrammarException>(() => Grammar<char>.Custom(undeclared)).Message);
    }

    [Fact]
    public void RejectsANullScannerAndSaysToDeclareItEarlier() =>
        Assert.Equal(
            "'Scanners.Missing' is null. If it refers to a scanner declared later in the class, declare the scanner before the rules that use it.",
            Assert.Throws<GrammarException>(() => Grammar<char>.Custom(Scanners.Missing)).Message
        );

    [Fact]
    public void RejectsFirstCharactersForInputOtherThanChar()
    {
        var bytes = new StubScanner<byte>(new(Nullable: false, First: CharSet.Of('r')));

        Assert.Equal(
            "'bytes' declares First characters, but it reads Byte, not char. Leave First null for a scanner over other items.",
            Assert.Throws<GrammarException>(() => Grammar<byte>.Custom(bytes)).Message
        );
    }

    [Fact]
    public void WithoutAFirstSetTheNodeIsOpaqueAndMatchesAnyItem()
    {
        var chars = Node(Grammar<char>.Custom(new StubScanner<char>(new(Nullable: false))));
        var bytes = Node(Grammar<byte>.Custom(new StubScanner<byte>(new(Nullable: false))));

        Assert.Equal(FirstSet.AnyItem, chars.First);
        Assert.True(chars.Opaque);
        Assert.Null(chars.Name);
        Assert.Equal(FirstSet.AnyItem, bytes.First);
        Assert.True(bytes.Opaque);
    }
}
