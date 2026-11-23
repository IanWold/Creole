namespace Creole;

/// <summary>
/// What a plan saves before it runs something it may have to undo, and puts back when it does.
/// </summary>
/// <remarks>
/// <para>
/// A plan that takes marks has a mark type as a type parameter, and lowering chooses it for the whole grammar.
/// A grammar whose parsers cannot change a part of the layer's parse state takes a mark that leaves that part out.
/// The mark is a struct, so each choice compiles to its own code and a mark that saves less costs nothing for the parts it leaves out.
/// </para>
/// <para>
/// Choices and repetitions take a mark before each attempt and call <see cref="FailedEmpty"/> when it fails.
/// <c>Atomic</c>, <c>Lookahead</c>, <c>Not</c> and <c>Longest</c> take one at their start and call <see cref="Rewind"/> wherever they go back to it.
/// They restore the position and the cut flag themselves, because each has its own rule for the cut.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The mark type itself.</typeparam>
internal interface IMark<TSelf>
    where TSelf : struct, IMark<TSelf>
{
    /// <summary>
    /// The position the mark was taken at.
    /// </summary>
    long Position { get; }

    /// <summary>
    /// Call after the attempt failed. Returns whether it failed without consuming, and if so puts back the cut flag and every other part the mark saved.
    /// </summary>
    bool FailedEmpty<T>(ref Cursor<T> cursor);

    /// <summary>
    /// Puts back every part the mark saved other than the position and the cut flag, as a construct that goes back to the mark does.
    /// </summary>
    void Rewind<T>(ref Cursor<T> cursor);

    /// <summary>
    /// Saves what the mark holds from the cursor.
    /// </summary>
    static abstract TSelf Take<T>(ref Cursor<T> cursor);
}
