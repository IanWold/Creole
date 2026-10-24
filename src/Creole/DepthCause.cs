namespace Creole;

/// <summary>
/// Why a parse went too deep to go on.
/// </summary>
internal enum DepthCause
{
    /// <summary>
    /// The parse was already its cursor's <see cref="Cursor{T}.MaxDepth"/> levels deep.
    /// </summary>
    MaxDepth,

    /// <summary>
    /// The thread was short of stack before the parse reached its cursor's <see cref="Cursor{T}.MaxDepth"/>.
    /// </summary>
    Stack,
}
