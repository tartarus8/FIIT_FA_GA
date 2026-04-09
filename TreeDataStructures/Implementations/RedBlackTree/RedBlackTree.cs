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

    protected override void RemoveNode(RbNode<TKey, TValue> node)
    {
        var z = node;
        var y = z;
        var yOriginalColor = y.Color;
        RbNode<TKey, TValue>? x;
        RbNode<TKey, TValue>? xParent;

        if (z.Left == null) {
            x = z.Right;
            xParent = z.Parent;
            Transplant(z, z.Right);
        }
        else if (z.Right == null) {
            x = z.Left;
            xParent = z.Parent;
            Transplant(z, z.Left);
        }
        else {
            y = Minimum(z.Right);
            yOriginalColor = y.Color;
            x = y.Right;
            if (y.Parent == z) {
                xParent = y;
            }
            else {
                xParent = y.Parent;
                Transplant(y, y.Right);
                y.Right = z.Right;
                y.Right.Parent = y;
            }
            Transplant(z, y);
            y.Left = z.Left;
            y.Left.Parent = y;
            y.Color = z.Color;
        }

        if (yOriginalColor == RbColor.Black) {
            RemoveFixup(x, xParent);
        }
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
    }

    private void RemoveFixup(RbNode<TKey, TValue>? x, RbNode<TKey, TValue>? xParent)
    {
        while (x != Root && IsBlack(x)) {
            if (x == xParent!.Left) {
                var w = xParent.Right!;
                if (IsRed(w)) {
                    w.Color = RbColor.Black;
                    xParent.Color = RbColor.Red;
                    RotateLeft(xParent);
                    w = xParent.Right!;
                }

                if (IsBlack(w.Left) && IsBlack(w.Right)) {
                    w.Color = RbColor.Red;
                    x = xParent;
                    xParent = x.Parent;
                }
                else {
                    if (IsBlack(w.Right)) {
                        if (w.Left != null) {
                            w.Left.Color = RbColor.Black;
                        }
                        w.Color = RbColor.Red;
                        RotateRight(w);
                        w = xParent.Right!;
                    }

                    w.Color = xParent.Color;
                    xParent.Color = RbColor.Black;
                    if (w.Right != null) {
                        w.Right.Color = RbColor.Black;
                    }
                    RotateLeft(xParent);
                    x = Root;
                }
            }
            else {
                var w = xParent!.Left!;
                if (IsRed(w)) {
                    w.Color = RbColor.Black;
                    xParent.Color = RbColor.Red;
                    RotateRight(xParent);
                    w = xParent.Left!;
                }

                if (IsBlack(w.Right) && IsBlack(w.Left)) {
                    w.Color = RbColor.Red;
                    x = xParent;
                    xParent = x.Parent;
                }
                else {
                    if (IsBlack(w.Left)) {
                        if (w.Right != null) {
                            w.Right.Color = RbColor.Black;
                        }
                        w.Color = RbColor.Red;
                        RotateLeft(w);
                        w = xParent.Left!;
                    }

                    w.Color = xParent.Color;
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
