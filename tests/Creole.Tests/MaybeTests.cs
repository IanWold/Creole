using static Creole.Grammar<char>;

namespace Creole.Tests;

public class MaybeTests
{
    private static readonly Parser<char, int> _int = Return(1);

    private static string Describe(Maybe<int> maybe) => maybe switch
    {
        int value => $"just {value}",
        Nothing => "nothing",
    };

    [Fact]
    public void AJustHoldsItsValueEvenIfNull()
    {
        Maybe<string?> just = new(null);

        Assert.Equal("just", just switch { string => "just", Nothing => "nothing", null => "null" });
        Assert.Null(just.GetValueOrDefault("fallback"));
        Assert.Equal("Just()", just.ToString());
    }

    [Fact]
    public void ASwitchMissingNothingFailsTheBuild()
    {
        var errors = SnippetCompiler.Compile("Outsider", """
            static class Consumer
            {
                static int Read(Creole.Maybe<int> maybe) => maybe switch { int value => value };
            }
            """
        );

        Assert.Equal(["CS8509"], errors);
    }

    [Fact]
    public void ASwitchOverBothCasesCompiles()
    {
        var errors = SnippetCompiler.Compile("Outsider", """
            static class Consumer
            {
                static int Read(Creole.Maybe<int> maybe) => maybe switch { int value => value, Creole.Nothing => 0 };
            }
            """
        );

        Assert.Empty(errors);
    }

    [Fact]
    public void ConversionsMakeEachCase()
    {
        Maybe<int> just = 3;
        Maybe<int> nothing = new Nothing();

        Assert.Equal("just 3", Describe(just));
        Assert.Equal("nothing", Describe(nothing));
        Assert.Equal(3, just.Value);
        Assert.IsType<Nothing>(nothing.Value);
    }

    [Fact]
    public void CreatingAndMatchingDoesNotAllocate()
    {
        var total = 0;
        void Run()
        {
            for (var i = 0; i < 100; i++)
            {
                Maybe<int> maybe = i % 2 == 0 ? i : new Nothing();
                total += maybe switch { int value => value, Nothing => 1 };
            }
        }

        Run();

        Assert.Equal(0, Allocation.Fewest(Run));
        Assert.NotEqual(0, total);
    }

    [Fact]
    public void DefaultIsNothing()
    {
        var nothing = default(Maybe<int>);

        Assert.Equal("nothing", Describe(nothing));
        Assert.Equal(new Nothing(), nothing);
        Assert.Equal(0, nothing.GetValueOrDefault());
        Assert.Equal(7, nothing.GetValueOrDefault(7));
        Assert.Equal("Nothing", nothing.ToString());
    }

    [Fact]
    public void EqualityComparesTheCaseThenTheValue()
    {
        Assert.Equal<Maybe<int>>(new Nothing(), default);
        Assert.Equal<Maybe<int>>(1, 1);
        Assert.NotEqual<Maybe<int>>(1, 2);
        Assert.NotEqual<Maybe<int>>(0, new Nothing());
    }

    [Fact]
    public void NullReceiverOfOptionalNamesTheExpression()
    {
        Parser<char, int> sign = null!;

        Assert.Equal(
            "'sign' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => sign).",
            Assert.Throws<GrammarException>(() => sign.Optional()).Message
        );
    }

    [Fact]
    public void OptionalBuildsOneOptionalNode()
    {
        INode node = Assert.IsType<OptionalNode<char, int>>(_int.Optional());

        Assert.Equal(NodeKind.Optional, node.Kind);
        Assert.Equal([_int], node.Children);
    }

    [Fact]
    public void OptionalWorksForReferenceAndValueTypes()
    {
        Assert.IsType<OptionalNode<char, string>>(Return("x").Optional());
        Assert.IsType<OptionalNode<char, int?>>(Return<int?>(null).Optional());
    }
}
