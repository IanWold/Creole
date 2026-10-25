namespace Creole;

/// <summary>
/// A <c>Named</c> node seen without its type arguments: what dispatch needs to record for an alternative it skips.
/// </summary>
internal interface ILabelNode : INode
{
    /// <summary>
    /// What a failure expects once the label replaces what the node's parser recorded.
    /// </summary>
    Expected Expected { get; }
}
