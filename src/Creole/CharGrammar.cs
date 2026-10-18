namespace Creole;

/// <summary>
/// The base class for grammars over characters, such as lexers.
/// </summary>
public abstract class CharGrammar : Grammar<char>
{
    /// <summary>
    /// Creates a grammar. Grammar classes hold static rules and are not usually instantiated.
    /// </summary>
    protected CharGrammar() { }
}
