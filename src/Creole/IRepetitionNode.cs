namespace Creole;

/// <summary>
/// A repetition node's bounds, readable without knowing its item type. Its item is <see cref="INode.Children"/>[0].
/// </summary>
internal interface IRepetitionNode : INode
{
    int? Max { get; }

    int Min { get; }
}
