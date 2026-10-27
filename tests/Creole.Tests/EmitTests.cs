namespace Creole.Tests;

file interface IShape;

file sealed record Square : IShape;

public class EmitTests
{
    private static Emitted<char> Derived(char item) => new(item, IsSynthetic: false);

    private static List<Emitted<T>> Outputs<T>(Emit<T> emit)
    {
        var outputs = new List<Emitted<T>>();

        foreach (var output in emit)
        {
            outputs.Add(output);
        }

        return outputs;
    }

    private static Emitted<char> Synthetic(char item) => new(item, IsSynthetic: true);

    [Fact]
    public void ACollectionExpressionBuildsItsOutputsInOrder()
    {
        Emit<char> emit = ['a', Emit.Synthetic('b'), 'c'];

        Assert.Equal(3, emit.Count);
        Assert.Equal([Derived('a'), Synthetic('b'), Derived('c')], Outputs(emit));
    }

    [Fact]
    public void AnInterfaceTypedItemDoesNotConvert() =>
        Assert.Equal(["CS0029"], SnippetCompiler.Compile("Outsider", """
            interface IShape;

            static class Consumer
            {
                static Creole.Emit<IShape> Wrap(IShape shape) => shape;
            }
            """
        ));

    [Fact]
    public void AnItemConvertsToOneDerivedOutput()
    {
        Emit<char> assigned = 'a';
        Func<char, Emit<char>> returned = static item => item;

        Assert.Equal([Derived('a')], Outputs(assigned));
        Assert.Equal([Derived('b')], Outputs(returned('b')));
    }

    [Fact]
    public void BuildingAndReadingZeroOrOneOutputDoesNotAllocate()
    {
        var total = 0;

        void Run()
        {
            for (var i = 0; i < 100; i++)
            {
                Emit<int> emit = (i % 4) switch
                {
                    0 => [],
                    1 => i,
                    2 => Emit.Synthetic(i),
                    _ => [i],
                };

                foreach (var output in emit)
                {
                    total += output.Item;
                }
            }
        }

        Run();

        Assert.Equal(0, Allocation.Fewest(Run));
        Assert.NotEqual(0, total);
    }

    [Fact]
    public void EmitsAreEqualWhenTheirOutputsAreEqualInOrder()
    {
        Emit<char> converted = 'a';
        Emit<char> listed = ['a'];
        Emit<char> pair = ['a', 'b'];

        Assert.Equal(converted, listed);
        Assert.Equal(converted, Emit.One('a'));
        Assert.Equal(converted.GetHashCode(), listed.GetHashCode());
        Assert.True(pair == ['a', 'b']);
        Assert.Equal(pair.GetHashCode(), ((Emit<char>)['a', 'b']).GetHashCode());
        Assert.True(pair != ['b', 'a']);
        Assert.NotEqual(converted, Emit.Synthetic('a'));
        Assert.NotEqual(converted, pair);
        Assert.Equal(default, (Emit<char>)[]);
    }

    [Fact]
    public void EmptyAndDefaultHoldNoOutputs()
    {
        Emit<char> empty = [];

        Assert.Equal(0, empty.Count);
        Assert.Empty(Outputs(empty));
        Assert.Equal(0, default(Emit<char>).Count);
        Assert.Empty(Outputs(default(Emit<char>)));
    }

    [Fact]
    public void OneMakesADerivedOutputOfAnInterfaceTypedItem()
    {
        IShape shape = new Square();

        Assert.Equal([new Emitted<IShape>(shape, IsSynthetic: false)], Outputs(Emit.One(shape)));
    }

    [Fact]
    public void SpreadsAddEachOutputAndKeepItsMark()
    {
        Emit<char> closing = [Emit.Synthetic('x'), 'y'];
        Emit<char> emit = ['a', .. closing, .. Enumerable.Repeat(Emit.Synthetic('z'), 2)];

        Assert.Equal([Derived('a'), Synthetic('x'), Derived('y'), Synthetic('z'), Synthetic('z')], Outputs(emit));
    }

    [Fact]
    public void SyntheticMakesOneSyntheticOutput()
    {
        Emit<char> emit = Emit.Synthetic('a');

        Assert.Equal([Synthetic('a')], Outputs(emit));
    }

    [Fact]
    public void ToStringMarksSyntheticOutputs() =>
        Assert.Equal("[a, b (synthetic)]", ((Emit<char>)['a', Emit.Synthetic('b')]).ToString());
}
