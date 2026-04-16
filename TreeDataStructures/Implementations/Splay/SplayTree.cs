using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
{
    public SplayTree() : base(null) { }
    public SplayTree(IComparer<TKey>? comparer) : base(comparer) { }

    private void Splay(BstNode<TKey, TValue>? x)
    {
        while (x?.Parent != null)
        {
            var p = x.Parent;
            var g = p.Parent;
            if (g == null) {
                if (x.IsLeftChild) {
                    RotateRight(p);
                }
                else {
                    RotateLeft(p);
                }
            }
            else if (x.IsLeftChild && p.IsLeftChild) {
                RotateRight(g);
                RotateRight(p);
            }
            else if (x.IsRightChild && p.IsRightChild) {
                RotateLeft(g);
                RotateLeft(p);
            }
            else if (x.IsLeftChild && p.IsRightChild) {
                RotateRight(p);
                RotateLeft(g);
            }
            else {
                RotateLeft(p);
                RotateRight(g);
            }
        }
        Root = x;
    }

    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = FindNode(key);
        if (node != null) {
            Splay(node);
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node != null) {
            Splay(node);
            return true;
        }
        return false;
    }

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode) => Splay(newNode);

    protected override void OnNodeRemoved(BstNode<TKey, TValue> unlinkedNode, BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child) {
        if (parent != null) {
            Splay(parent);
        }
        base.OnNodeRemoved(unlinkedNode, parent, child);
    }
}
