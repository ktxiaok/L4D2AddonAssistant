﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;

namespace L4D2AddonAssistant
{
    public static class IHierarchyNodeExtensions
    {
        private class EnumerableWrapper<T> : IEnumerable<T> where T : IHierarchyNode<T>
        {
            private readonly Func<IEnumerator<T>> _getEnumerator;

            public EnumerableWrapper(Func<IEnumerator<T>> getEnumerator)
            {
                _getEnumerator = getEnumerator;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _getEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _getEnumerator();
            }
        }

        private class HierarchyPreorderDfs<T> : IHierarchyPreorderDfs<T> where T : IHierarchyNode<T>
        {
            private readonly IEnumerator<T> _enumerator;
            private readonly Action _skip;

            public HierarchyPreorderDfs(IEnumerator<T> enumerator, Action skip)
            {
                _enumerator = enumerator;
                _skip = skip;
            }

            public T Current => _enumerator.Current;

            object IEnumerator.Current => _enumerator.Current;

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            public void SkipDescendantsOfCurrent()
            {
                _skip();
            }
        }

        public static IEnumerable<T> GetDescendantsByDfsPreorder<T>(this IHierarchyNode<T> node) where T : IHierarchyNode<T>
        {
            return new EnumerableWrapper<T>(() => GetDescendantsEnumByDfsPreorder(node));
        }

        public static IEnumerable<T> GetSelfAndDescendantsByDfsPreorder<T>(this IHierarchyNode<T> node) where T : IHierarchyNode<T>
        {
            return new EnumerableWrapper<T>(() => GetSelfAndDescendantsEnumByDfsPreorder(node));
        }

        public static IHierarchyPreorderDfs<T> GetDescendantsEnumByDfsPreorder<T>(this IHierarchyNode<T> node) where T : IHierarchyNode<T>
        {
            bool needSkip = false;
            var enumerator = GetEnumerator();
            IEnumerator<T> GetEnumerator()
            {
                if (node.IsNonterminal)
                {
                    List<IEnumerator<T>> stack = new() { node.Children.GetEnumerator() };
                    while (stack.Count > 0)
                    {
                        IEnumerator<T> current = stack[stack.Count - 1];
                        if (current.MoveNext())
                        {
                            T child = current.Current;
                            yield return child;
                            if (!needSkip && child.IsNonterminal)
                            {
                                stack.Add(child.Children.GetEnumerator());
                            }
                            needSkip = false;
                        }
                        else
                        {
                            stack.RemoveAt(stack.Count - 1);
                        }
                    }
                }
            }
            var skip = () =>
            {
                needSkip = true;
            };
            return new HierarchyPreorderDfs<T>(enumerator, skip);
        }

        public static IHierarchyPreorderDfs<T> GetSelfAndDescendantsEnumByDfsPreorder<T>(this IHierarchyNode<T> node) where T : IHierarchyNode<T>
        {
            Lazy<IHierarchyPreorderDfs<T>> dfsLazy = new(() => node.GetDescendantsEnumByDfsPreorder(), false);
            bool needSkip = false;
            IEnumerator<T> GetEnumerator()
            {
                yield return (T)node;
                if (needSkip)
                {
                    yield break;
                }
                var dfs = dfsLazy.Value;
                while (dfs.MoveNext())
                {
                    yield return dfs.Current;
                }
            }
            var enumerator = GetEnumerator();
            var skip = () =>
            {
                if (dfsLazy.IsValueCreated)
                {
                    dfsLazy.Value.SkipDescendantsOfCurrent();
                }
                else
                {
                    needSkip = true;
                }
            };
            return new HierarchyPreorderDfs<T>(enumerator, skip);
        }

        public static IEnumerable<T> GetSelfAndDescendantsByDfsPostorder<T>(this IHierarchyNode<T> node) where T : IHierarchyNode<T>
        {
            if (!node.IsNonterminal)
            {
                yield return (T)node;
            }
            List<(T parent, IEnumerator<T> children)> stack = new() { ((T)node, node.Children.GetEnumerator()) };
            while (stack.Count > 0)
            {
                int lastIndex = stack.Count - 1;
                var current = stack[lastIndex];
                if (current.children.MoveNext())
                {
                    T child = current.children.Current;
                    if (child.IsNonterminal)
                    {
                        stack.Add((child, child.Children.GetEnumerator()));
                    }
                    else
                    {
                        yield return child;
                    }
                }
                else
                {
                    yield return current.parent;
                    stack.RemoveAt(lastIndex);
                }
            }
        }
    }

    public interface IHierarchyPreorderDfs<T> : IEnumerator<T> where T : IHierarchyNode<T>
    {
        void SkipDescendantsOfCurrent();
    }
}
