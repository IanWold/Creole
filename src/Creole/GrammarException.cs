namespace Creole;

/// <summary>
/// The exception thrown when a grammar is built incorrectly, for example when a combinator receives a null
/// parser.
/// </summary>
/// <remarks>
/// A null parser argument usually means a field initializer refers to a rule declared later in the class,
/// which is still null when the initializer runs. Wrap the reference in <c>Rec(() =&gt; Rule)</c>.
/// </remarks>
public sealed class GrammarException : Exception
{
    /// <summary>
    /// Creates an exception with a default message.
    /// </summary>
    public GrammarException() : base("The grammar is not valid.") { }

    /// <summary>
    /// Creates an exception with the given message.
    /// </summary>
    /// <param name="message">What is wrong with the grammar.</param>
    public GrammarException(string message) : base(message) { }

    /// <summary>
    /// Creates an exception with the given message and cause.
    /// </summary>
    /// <param name="message">What is wrong with the grammar.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public GrammarException(string message, Exception innerException) : base(message, innerException) { }
}
