using System;
using System.Collections.Generic;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
{
    public RedBlackTree() : base(null) { }
    public RedBlackTree(IComparer<TKey>? comparer) : base(comparer) { }

    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RbNode<TKey, TValue>(key, value) {
            Color = RbColor.Red
        };
    }

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        var x = newNode;

        while (x != Root && IsRed(x.Parent)) {
            var parent = x.Parent!;
            var grandparent = parent.Parent!;

            if (parent.IsLeftChild) {
                var y = grandparent.Right;
                if (IsRed(y)) {
                    parent.Color = RbColor.Black;
                    y!.Color = RbColor.Black;
                    grandparent.Color = RbColor.Red;
                    x = grandparent;
                }
                else {
                    if (x.IsRightChild) {
                        x = parent;
                        RotateLeft(x);
                        parent = x.Parent!;
                    }
                    parent.Color = RbColor.Black;
                    grandparent.Color = RbColor.Red;
                    RotateRight(grandparent);
                }
            }
            else {
                var y = grandparent.Left;
                if (IsRed(y)) {
                    parent.Color = RbColor.Black;
                    y!.Color = RbColor.Black;
                    grandparent.Color = RbColor.Red;
                    x = grandparent;
                }
                else {
                    if (x.IsLeftChild) {
                        x = parent;
                        RotateRight(x);
                        parent = x.Parent!;
                    }
                    parent.Color = RbColor.Black;
                    grandparent.Color = RbColor.Red;
                    RotateLeft(grandparent);
                }
            }
        }
        Root!.Color = RbColor.Black;
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue> unlinkedNode, RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? replacement)
{
    bool wasTwoChild = unlinkedNode.Left != null && unlinkedNode.Right != null;

    RbNode<TKey, TValue>? x;
    RbNode<TKey, TValue>? xParent = parent;
    RbColor physicalRemovedColor;
    bool isLeft;

    if (wasTwoChild) {
        x = replacement!.Right;
        physicalRemovedColor = replacement.Color;
        replacement.Color = unlinkedNode.Color;
        isLeft = (xParent != replacement);
    }
    else {
        x = replacement;
        physicalRemovedColor = unlinkedNode.Color;
        isLeft = (xParent != null && unlinkedNode == xParent.Left);
    }

    if (physicalRemovedColor == RbColor.Black) {
        RemoveFixup(x, xParent, isLeft);
    }

    base.OnNodeRemoved(unlinkedNode, parent, replacement);
}

private void RemoveFixup(RbNode<TKey, TValue>? x, RbNode<TKey, TValue>? xParent, bool isLeft)
{
    while (x != Root && IsBlack(x)) {
        if (xParent == null) {
            break;
        }
        if (isLeft) {
            var w = xParent.Right;
            if (IsRed(w)) {
                w!.Color = RbColor.Black;
                xParent.Color = RbColor.Red;
                RotateLeft(xParent);
                w = xParent.Right;
            }

            if (w == null || (IsBlack(w.Left) && IsBlack(w.Right))) {
                if (w != null) {
                    w.Color = RbColor.Red;
                }
                x = xParent;
                xParent = x.Parent;
                if (xParent != null) {
                    isLeft = (x == xParent.Left);
                }
            }
            else {
                if (IsBlack(w.Right)) {
                    if (w.Left != null) {
                        w.Left.Color = RbColor.Black;
                    }
                    w.Color = RbColor.Red;
                    RotateRight(w);
                    w = xParent.Right;
                }

                w!.Color = xParent.Color;
                xParent.Color = RbColor.Black;
                if (w.Right != null) {
                    w.Right.Color = RbColor.Black;
                }
                RotateLeft(xParent);
                x = Root;
            }
        }
        else {
            var w = xParent.Left;
            if (IsRed(w)) {
                w!.Color = RbColor.Black;
                xParent.Color = RbColor.Red;
                RotateRight(xParent);
                w = xParent.Left;
            }

            if (w == null || (IsBlack(w.Right) && IsBlack(w.Left))) {
                if (w != null) {
                    w.Color = RbColor.Red;
                }
                x = xParent;
                xParent = x.Parent;
                if (xParent != null) {
                    isLeft = (x == xParent.Left);
                }
            }
            else {
                if (IsBlack(w.Left)) {
                    if (w.Right != null) w.Right.Color = RbColor.Black;
                    w.Color = RbColor.Red;
                    RotateLeft(w);
                    w = xParent.Left;
                }

                w!.Color = xParent.Color;
                xParent.Color = RbColor.Black;
                if (w.Left != null) {
                    w.Left.Color = RbColor.Black;
                }
                RotateRight(xParent);
                x = Root;
            }
        }
    }

    if (x != null) {
        x.Color = RbColor.Black;
    }
}

    private static bool IsRed(RbNode<TKey, TValue>? node) => node != null && node.Color == RbColor.Red;
    private static bool IsBlack(RbNode<TKey, TValue>? node) => node == null || node.Color == RbColor.Black;
}
