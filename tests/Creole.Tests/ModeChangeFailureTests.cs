using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

/// <summary>
/// How a change of mode makes a layer forget its failures past the cursor, shown in a parser run on its own, whose writes nothing reads.
/// </summary>
/// <remarks>
/// In each case an <c>Atomic</c> reads <c>ab</c> of <c>abx</c> and fails at 2 expecting <c>c</c>, and then a write at 0 comes before a parser that fails there expecting <c>z</c>.
/// A write that changes the mode lowers the failure at 2 to one at 0, where <c>z</c> is expected. One that keeps the mode leaves it, and the error stays at 2.
/// </remarks>
public class ModeChangeFailureTests
{
    private enum Mode
    {
        A,
        B,
    }

    private static readonly ModeChannel<Mode> _modes = new(Mode.A);
    private static readonly Parser<char, char> _abc = Atomic(Char('a').Then(Char('b')).Then(Char('c')));

    private static readonly Dictionary<string, Parser<char, char>> _writes = new()
    {
        ["SetMode to another mode"] = _abc.Or(SetMode(_modes, Mode.B).Then(Char('z'))),
        ["SetMode to the same mode"] = _abc.Or(SetMode(_modes, Mode.A).Then(Char('z'))),
        ["PushMode of another mode"] = _abc.Or(PushMode(_modes, Mode.B).Then(Char('z'))),
        ["PushMode of the same mode"] = _abc.Or(PushMode(_modes, Mode.A).Then(Char('z'))),
        ["PopMode to another mode"] = PushMode(_modes, Mode.B).Then(_abc.Or(PopMode(_modes).Then(Char('z')))),
        ["PopMode to the same mode"] = PushMode(_modes, Mode.A).Then(_abc.Or(PopMode(_modes).Then(Char('z')))),
    };

    private static ParseError At(long position, char found, params Expectation[] expected) =>
        new(new SourceSpan(SourceId.Unnamed, position, position + 1), "parser", new UnexpectedItem($"'{found}'"), expected, []);

    [Fact]
    public void AFiltersRestoreForgetsTheFailuresPastAChangeMadeInsideIt() =>
        Assert.Equal(
            At(2, 'b'),
            Execution.Error(Char('x').Then(_abc.Optional()).Then(Char('a').Then(SetMode(_modes, Mode.B)).Then(Char('b')).Where(static _ => false)), "xabq")
        );

    [Fact]
    public void AFiltersRestoreKeepsTheFailuresPastAChangeMadeBeforeIt() =>
        Assert.Equal(
            At(3, 'q', new ExpectedItem("'c'")),
            Execution.Error(Char('x').Then(SetMode(_modes, Mode.B)).Then(_abc.Optional()).Then(Char('a').Where(static _ => false)), "xabq")
        );

    [Fact]
    public void AFiltersRestoreKeepsTheFailuresPastAWriteThatKeepsTheMode() =>
        Assert.Equal(
            At(3, 'q', new ExpectedItem("'c'")),
            Execution.Error(Char('x').Then(_abc.Optional()).Then(SetMode(_modes, Mode.A).Then(Char('a')).Where(static _ => false)), "xabq")
        );

    [Theory]
    [InlineData("SetMode to another mode")]
    [InlineData("PushMode of another mode")]
    [InlineData("PopMode to another mode")]
    public void AWriteThatChangesTheModeForgetsTheFailuresPastTheCursor(string write) =>
        Assert.Equal(At(0, 'a', new ExpectedItem("'z'")), Execution.Error(_writes[write], "abx"));

    [Theory]
    [InlineData("SetMode to the same mode")]
    [InlineData("PushMode of the same mode")]
    [InlineData("PopMode to the same mode")]
    public void AWriteThatKeepsTheModeForgetsNothing(string write) =>
        Assert.Equal(At(2, 'x', new ExpectedItem("'c'")), Execution.Error(_writes[write], "abx"));
}
