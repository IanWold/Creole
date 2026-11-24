using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

/// <summary>
/// The state of a <c>Layer.Tokens</c> layer, carried from step to step (§ 2. Layer-local state).
/// </summary>
public class LayerStateTests
{
    private static readonly Parser<char, Unit> _nothing = Return(new Unit());
    private static readonly Parser<char, string> _word = Letter.AtLeastOnce().Text();
    /// <summary>
    /// The code sample in docs/concepts/layer-state.md § Where the state starts. Keep the two in step.
    /// </summary>
    private static readonly Layer<char, string> _numbered = Layer.Tokens(0, _word.Scan<int, string>(static (word, count, _) => ($"{count}:{word}", count + 1)), Whitespace.SkipMany());

    [Fact]
    public void ALayerGivenANullInitialStateReadsItAsNull() =>
        Assert.Equal([null], LayerRun.Values(Layer.Tokens<char, string?, string?>(null, Letter.Then(GetState<string?>()), _nothing), "a"));

    [Fact]
    public void ALayerWithoutAnInitialStateStartsWithNoneAndKeepsOneItsGrammarSets()
    {
        var layer = Layer.Tokens(OneOf(Char('s').Then(SetState("set")).As("s"), Char('r').Then(GetState<string>())));

        Assert.Equal(["s", "set"], LayerRun.Values(layer, "sr"));
        Assert.Equal(
            "The lexer layer has no state, but its grammar reads it as String. Give the layer an initialState, or set the state with SetState before reading it.",
            Assert.Throws<GrammarException>(() => LayerRun.Values(layer, "r")).Message
        );
    }

    [Fact]
    public void ALayerWithoutStateRecordsNoStateBeforeItsItems() =>
        Assert.All(LayerRun.Items(Layer.Tokens(_word, skip: Whitespace.SkipMany()), "ab cd"), static entry => Assert.Null(entry.Slot.StateBefore));

    [Fact]
    public void AStepThatLeavesAStateOfAnotherTypeThrows()
    {
        Assert.Equal(
            "A step of the lexer layer left its state of type String, but its initialState is of type Int32.",
            Assert.Throws<GrammarException>(() => LayerRun.Values(Layer.Tokens(0, Letter.Then(SetState("x")).As("a"), _nothing), "a")).Message
        );
        Assert.Equal(
            "A step of the lexer layer left its state null, but its initialState is of type Int32, which cannot be null.",
            Assert.Throws<GrammarException>(() => LayerRun.Values(Layer.Tokens(0, Letter.Then(SetState<string?>(null)).As("a"), _nothing), "a")).Message
        );
        Assert.Equal(["a", "a"], LayerRun.Values(Layer.Tokens<char, string, int?>(0, Letter.Then(SetState<int?>(null)).As("a"), _nothing), "aa"));
    }

    [Fact]
    public void BacktrackingInsideAStepPutsTheStateBack()
    {
        var item = Atomic(Char('a').Scan<int, char>(static (a, count, _) => (a, count + 1)).Then(Char('b'))).Or(Char('a')).Then(GetState<int>()).Select(static count => $"{count}");

        Assert.Equal(["0", "0"], LayerRun.Values(Layer.Tokens(0, item, _nothing), "aa"));
    }

    [Fact]
    public void EachItemsSlotRecordsTheStateItsStepStartedWith() =>
        Assert.Equal([0, 1, 2], LayerRun.Items(_numbered, "ab cd e").Select(static entry => entry.Slot.StateBefore));

    [Fact]
    public void EachStepStartsWithTheStateTheStepBeforeLeft() =>
        Assert.Equal(["0:ab", "1:cd", "2:e"], LayerRun.Values(_numbered, "ab cd e"));

    [Fact]
    public void NullArgumentsThrow()
    {
        Assert.Throws<GrammarException>(static () => Layer.Tokens<char, string, int>(0, null!, _nothing));
        Assert.Throws<GrammarException>(static () => Layer.Tokens(0, _word, null!));
        Assert.Throws<GrammarException>(static () => Layer.Tokens(0, _word, _nothing, name: ""));
    }

    [Fact]
    public void ReadingTheStateAsAnotherTypeNamesTheLayer() =>
        Assert.Equal(
            "The words layer's state is of type Int32, but its grammar reads it as String.",
            Assert.Throws<GrammarException>(() => LayerRun.Values(Layer.Tokens(0, Letter.Then(GetState<string>()), _nothing, name: "words"), "a")).Message
        );

    [Fact]
    public void SkipAndItemShareTheState()
    {
        var spaces = Char(' ').Scan<int, char>(static (space, count, _) => (space, count + 1)).SkipMany();

        Assert.Equal(["a1", "b3"], LayerRun.Values(Layer.Tokens(0, _word.Scan<int, string>(static (word, count, _) => ($"{word}{count}", count)), spaces), " a  b"));
    }
}
