using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode> : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; }
    public int Count { get; protected set; }
    public bool IsReadOnly => false;

    protected BinarySearchTreeBase(IComparer<TKey>? comparer = null)
    {
        Comparer = comparer ?? Comparer<TKey>.Default;
    }

    public virtual void Add(TKey key, TValue value)
    {
        if (Root == null) {
            Root = CreateNode(key, value);
            Count++;
            OnNodeAdded(Root);
            return;
        }
        TNode? current = Root;
        TNode? parent = null;
        int cmp = 0;
        while (current != null) {
            parent = current;
            cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) {
                current.Value = value;
                return;
                }
            current = cmp < 0 ? current.Left : current.Right;
        }
        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;
        if (cmp < 0) {
            parent!.Left = newNode;
        }
        else {
            parent!.Right = newNode;
        }
        Count++;
        OnNodeAdded(newNode);
    }

    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) {
            return false;
        }
        RemoveNode(node);
        Count--;
        return true;
    }

    protected virtual void RemoveNode(TNode node)
    {
        TNode? parent = node.Parent;
        TNode? replacement;
        if (node.Left == null) {
            replacement = node.Right;
            Transplant(node, node.Right);
        }
        else if (node.Right == null) {
            replacement = node.Left;
            Transplant(node, node.Left);
        }
        else {
            TNode y = Minimum(node.Right);
            parent = y.Parent;
            if (y.Parent != node) {
                Transplant(y, y.Right);
                y.Right = node.Right;
                y.Right.Parent = y;
            }
            Transplant(node, y);
            y.Left = node.Left;
            y.Left.Parent = y;
            replacement = y;
            if (parent == node) {
                parent = y;
            }
        }

        node.Parent = null;
        node.Left = null;
        node.Right = null;
        OnNodeRemoved(parent, replacement);
    }

    protected TNode Minimum(TNode node)
    {
        while (node.Left != null) {
            node = node.Left;
        }
        return node;
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;

    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null) {
            value = node.Value; return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    protected abstract TNode CreateNode(TKey key, TValue value);
    protected virtual void OnNodeAdded(TNode newNode) {}
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) {}

    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null) {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) {
                return current;
            }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        TNode y = x.Right!;
        x.Right = y.Left;
        if (y.Left != null) {
            y.Left.Parent = x;
        }
        y.Parent = x.Parent;
        if (x.Parent == null) {
            Root = y;
        }
        else if (x.IsLeftChild) {
            x.Parent.Left = y;
        }
        else {
            x.Parent.Right = y;
        }
        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        TNode x = y.Left!;
        y.Left = x.Right;
        if (x.Right != null) {
            x.Right.Parent = y;
        }
        x.Parent = y.Parent;
        if (y.Parent == null) {
            Root = x;
        }
        else if (y.IsLeftChild) {
            y.Parent.Left = x;
        }
        else {
            y.Parent.Right = x;
        }
        x.Right = y;
        y.Parent = x;
    }

    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null) {
            Root = v;
        }
        else if (u.IsLeftChild) {
            u.Parent.Left = v;
        }
        else {
            u.Parent.Right = v;
        }
        if (v != null) {
            v.Parent = u.Parent;
        }
    }

    private static int GetSubtreeHeight(TNode? node)
    {
        if (node == null) {
            return 0;
        }
        return 1 + Math.Max(GetSubtreeHeight(node.Left), GetSubtreeHeight(node.Right));
    }

    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);

    private struct TreeIterator : IEnumerable<TreeEntry<TKey, TValue>>, IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;
        private Stack<TNode> _stack;
        private TNode? _current;
        private TNode? _lastVisited;
        private TNode? _prevNode;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _stack = new Stack<TNode>();
            Reset();
        }

        public TreeEntry<TKey, TValue> Current => new(_current!.Key, _current.Value, GetSubtreeHeight(_current));
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            return _strategy switch
            {
                TraversalStrategy.PreOrder => MovePrefix(true),
                TraversalStrategy.PostOrderReverse => MovePrefix(false),
                TraversalStrategy.InOrder => MoveInfix(true),
                TraversalStrategy.InOrderReverse => MoveInfix(false),
                TraversalStrategy.PostOrder => MovePostfix(true),
                TraversalStrategy.PreOrderReverse => MovePostfix(false),
                _ => false
            };
        }

        private bool MovePrefix(bool leftFirst)
        {
            if (_stack.Count == 0) {
                return false;
            }
            _current = _stack.Pop();
            TNode? first = leftFirst ? _current.Right : _current.Left;
            TNode? second = leftFirst ? _current.Left : _current.Right;
            if (first != null) {
                _stack.Push(first);
            }
            if (second != null) {
                _stack.Push(second);
            }
            return true;
        }

        private bool MoveInfix(bool leftFirst)
        {
            while (_stack.Count > 0 || _lastVisited != null) {
                if (_lastVisited != null) {
                    _stack.Push(_lastVisited);
                    _lastVisited = leftFirst ? _lastVisited.Left : _lastVisited.Right;
                }
                else {
                    _current = _stack.Pop();
                    _lastVisited = leftFirst ? _current.Right : _current.Left;
                    return true;
                }
            }
            return false;
        }

        private bool MovePostfix(bool leftFirst)
        {
            while (_stack.Count > 0 || _lastVisited != null)
            {
                if (_lastVisited != null) {
                    _stack.Push(_lastVisited);
                    _lastVisited = leftFirst ? _lastVisited.Left : _lastVisited.Right;
                    }
                else {
                    TNode peek = _stack.Peek();
                    TNode? next = leftFirst ? peek.Right : peek.Left;
                    if (next != null && _prevNode != next) {
                        _lastVisited = next;
                    }
                    else {
                        _current = _stack.Pop();
                        _prevNode = _current;
                        return true;
                    }
                }
            }
            return false;
        }

        public void Reset()
        {
            _stack.Clear();
            _current = null;
            _prevNode = null;
            if (_strategy == TraversalStrategy.PreOrder || _strategy == TraversalStrategy.PostOrderReverse) {
                if (_root != null) {
                    _stack.Push(_root);
                }
            }
            else {
                _lastVisited = _root;
            }
        }
        public void Dispose() { }
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
    }

    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    { foreach (var e in InOrder()) yield return new KeyValuePair<TKey, TValue>(e.Key, e.Value); }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) { foreach (var kvp in this) array[index++] = kvp; }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
    public ICollection<TKey> Keys => this.Select(x => x.Key).ToList();
    public ICollection<TValue> Values => this.Select(x => x.Value).ToList();
}
