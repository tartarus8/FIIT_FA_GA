using System;
using System.Collections.Generic;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    public AvlTree() : base(null) { }
    public AvlTree(IComparer<TKey>? comparer) : base(comparer) { }

    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        BalanceUpwards(newNode.Parent);
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue> unlinkedNode, AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        BalanceUpwards(parent);
        base.OnNodeRemoved(unlinkedNode, parent, child);
    }

    private void BalanceUpwards(AvlNode<TKey, TValue>? node)
    {
        while (node != null) {
            var next = node.Parent;

            UpdateHeight(node);
            int balance = GetBalance(node);

            if (balance > 1) {
                if (GetBalance(node.Left) < 0) {
                    var left = node.Left!;
                    RotateLeft(left);
                    UpdateHeight(left);
                    UpdateHeight(left.Parent!);
                }
                RotateRight(node);
                UpdateHeight(node);
                UpdateHeight(node.Parent!);
            }
            else if (balance < -1) {
                if (GetBalance(node.Right) > 0) {
                    var right = node.Right!;
                    RotateRight(right);
                    UpdateHeight(right);
                    UpdateHeight(right.Parent!);
                }

                RotateLeft(node);
                UpdateHeight(node);
                UpdateHeight(node.Parent!);
            }

            node = next;
        }
    }

    private static int GetHeight(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;

    private static int GetBalance(AvlNode<TKey, TValue>? node) =>
        node == null ? 0 : GetHeight(node.Left) - GetHeight(node.Right);

    private static void UpdateHeight(AvlNode<TKey, TValue> node) =>
        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
}
