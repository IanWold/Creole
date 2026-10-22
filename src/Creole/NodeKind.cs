namespace Creole;

/// <summary>
/// One member per node type.
/// Switch on it with a switch expression and no discard arm, so that adding a kind fails the build until every consumer handles it.
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
    Any,
    End,
    Token,
    Satisfy,
    Custom,
    CharClass,
    String,
    Kind,
    Case,
    Sequence,
    Map,
    Filter,
    Optional,
    Slice,
    Text,
    Or,
    Switch,
    Match,
    Longest,
    Atomic,
    Lookahead,
    Not,
    Cut,
    Named,
    Context,
    Rec,
    Bind,
}
