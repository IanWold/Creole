namespace Creole;

/// <summary>
/// One member per node type.
/// Switch on it with a switch expression and no discard arm, so that adding a kind fails the build until every consumer handles it (ADR-0001).
/// </summary>
internal enum NodeKind
{
    Return,
    Fail,
    Repeat,
    SkipRepeat,
    Separated,
    Until,
    Expression,
    Or,
    Switch,
}
