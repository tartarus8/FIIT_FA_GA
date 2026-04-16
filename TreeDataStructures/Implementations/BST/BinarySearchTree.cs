using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.BST;

public class BinarySearchTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, BstNode<TKey, TValue>>
{
    public BinarySearchTree() : base(null) { }
    public BinarySearchTree(IComparer<TKey>? comparer) : base(comparer) { }

    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode) { }
    protected override void OnNodeRemoved(BstNode<TKey, TValue> unlinkedNode, BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child) {
        base.OnNodeRemoved(unlinkedNode, parent, child);
    }
}
