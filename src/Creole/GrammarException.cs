namespace Creole;

/// <summary>
/// The exception thrown when a grammar is built incorrectly, for example when a combinator receives a null parser.
/// </summary>
/// <remarks>
/// <para>
/// A null parser argument usually means a field initializer refers to a rule declared later in the class, which is still null when the initializer runs.
/// Wrap the reference in <c>Rec(() =&gt; Rule)</c>.
/// </para>
/// <para>
/// Some faults show only later: a <c>Rec</c> whose rule is still null throws when the grammar is first used, and a repetition whose item succeeds without consuming input throws when the parse reaches it, because it would otherwise loop forever.
/// </para>
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
