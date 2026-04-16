using System;
using System.Collections.Generic;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
{
    public Treap() : base(null) { }
    public Treap(IComparer<TKey>? comparer) : base(comparer) { }

    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) {
            return (null, null);
        }
        if (Comparer.Compare(root.Key, key) <= 0) {
            var (left, right) = Split(root.Right, key);
            root.Right = left;

            if (left != null) {
                left.Parent = root;
            }
            if (right != null) {
                right.Parent = null;
            }
            return (root, right);
        }
        else {
            var (left, right) = Split(root.Left, key);
            root.Left = right;
            if (right != null) {
                right.Parent = root;
            }
            if (left != null) {
                left.Parent = null;
            }
            return (left, root);
        }
    }

    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) {
            return right;
        }
        if (right == null) {
            return left;
        }

        if (left.Priority > right.Priority) {
            left.Right = Merge(left.Right, right);
            if (left.Right != null) {
                left.Right.Parent = left;
            }
            return left;
        }
        else {
            right.Left = Merge(left, right.Left);
            if (right.Left != null) {
                right.Left.Parent = right;
            }
            return right;
        }
    }

    public override void Add(TKey key, TValue value)
    {
        var existing = FindNode(key);
        if (existing != null)
        {
            existing.Value = value;
            return;
        }

        var (left, right) = Split(Root, key);
        var newNode = CreateNode(key, value);

        var temp = Merge(left, newNode);
        Root = Merge(temp, right);

        if (Root != null) {
            Root.Parent = null;
        }
        Count++;
    }

    public override bool Remove(TKey key)
    {
        var node = FindNode(key);
        if (node == null) {
            return false;
        }

        var mergedSubtrees = Merge(node.Left, node.Right);

        if (node.Parent == null) {
            Root = mergedSubtrees;
        }
        else if (node.IsLeftChild) {
            node.Parent.Left = mergedSubtrees;
        }
        else {
            node.Parent.Right = mergedSubtrees;
        }

        if (mergedSubtrees != null) {
            mergedSubtrees.Parent = node.Parent;
        }

        node.Left = node.Right = node.Parent = null;
        Count--;

        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);

    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
    }

    protected override void OnNodeRemoved(TreapNode<TKey, TValue> unlinkedNode,TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
    }
}
