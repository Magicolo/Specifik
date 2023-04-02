using static Specifik.Node;

namespace Specifik.Parse;

public sealed record Tree(string Kind, ReadOnlyMemory<char> Value, Tree[] Trees);

delegate bool Parse(ref State state);

readonly record struct Build(
    Dictionary<Identifier, int> Indices,
    Dictionary<Node, int> References,
    List<Node?> Nodes);

record struct State(
    int Index,
    string Text,
    Stack<(int index, string kind)> Stack,
    Stack<(Tree tree, int depth)> Trees,
    Stack<Tree> Buffer,
    Parse[] References);

public sealed class Parser
{
    public static Tree[] Parse(Node node, string text) => From(node).Parse(text);

    public static Parser From(Node node)
    {
        var build = new Build(new(), new(), new());
        node = node.Descend(_ => _, node => Normalize(node, build));
        var references = new Parse?[build.Nodes.Count];
        for (int i = 0; i < build.Nodes.Count; i++) references[i] = Next(build.Nodes[i]!);
        return new(Next(node), references!);

        static Parse Next(Node node)
        {
            switch (node)
            {
                case True: return (ref State _) => true;
                case False: return (ref State _) => false;
                case And and:
                    var all = and.Flatten().Select(Next).ToArray();
                    return (ref State state) =>
                    {
                        foreach (var parse in all) if (!parse(ref state)) return false;
                        return true;
                    };
                case Or or:
                    var any = or.Flatten().Select(Next).ToArray();
                    return (ref State state) =>
                    {
                        foreach (var parse in any)
                        {
                            var stack = state.Stack.Count;
                            var trees = state.Trees.Count;
                            if (parse(ref state)) return true;
                            while (state.Stack.Count > stack) state.Stack.Pop();
                            while (state.Trees.Count > trees) state.Trees.Pop();
                        }
                        return false;
                    };
                case Refer(Identifier.Index(var index)): return (ref State state) => state.References[index](ref state);
                case Push(var kind):
                    return (ref State state) =>
                    {
                        state.Stack.Push((state.Index, kind));
                        return true;
                    };
                case Pop:
                    return (ref State state) =>
                    {
                        var depth = state.Stack.Count;
                        if (state.Stack.TryPop(out var frame))
                        {
                            while (state.Trees.TryPop(out var pair))
                            {
                                if (pair.depth > depth) state.Buffer.Push(pair.tree);
                                else { state.Trees.Push(pair); break; }
                            }

                            var tree = new Tree(frame.kind, state.Text.AsMemory(frame.index..state.Index), state.Buffer.ToArray());
                            state.Buffer.Clear();
                            state.Trees.Push((tree, depth));
                            return true;
                        }
                        else return false;
                    };
                case Text(var text):
                    return (ref State state) =>
                    {
                        if (state.Text.AsSpan(state.Index).StartsWith(text))
                        {
                            state.Index += text.Length;
                            return true;
                        }
                        else return false;
                    };
                default: throw new NotImplementedException();
            }
        }

        static int Define(Node node, Identifier identifier, Build build)
        {
            if (build.References.TryGetValue(node, out var index))
            {
                build.Nodes[Index(identifier, build)] = new Refer(new Identifier.Index(index));
                return index;
            }
            else
            {
                index = Index(identifier, build);
                build.Nodes[index] = node;
                build.References[node] = index;
                return index;
            }
        }

        static int Index(Identifier identifier, Build build)
        {
            if (identifier is Identifier.Index(var index) || build.Indices.TryGetValue(identifier, out index)) return index;
            index = build.Indices.Count;
            build.Nodes.Add(null);
            build.Indices.Add(identifier, index);
            return index;
        }

        /// <summary>
        /// Applies the following transformations:
        /// - True & x => x
        /// - x & True => x
        /// - False & x => False
        /// - x & False => False
        /// - x | x => x
        /// - False | x => x
        /// - x | False => x
        /// - True | x => x | True
        /// - Text("") => True
        /// - Text(x) & Text(y) => Text(x + y)
        /// </summary>
        static Node Normalize(Node node, Build build)
        {
            switch (node)
            {
                case And(True, var right): return right;
                case And(var left, True): return left;
                case And(False, _): return false;
                case And(_, False): return false;
                case Or(var left, var right) when left == right: return left;
                case Or(var left, False): return left;
                case Or(False, var right): return right;
                case Or(True, var right): return new Or(right, true);
                case Text(""): return true;
                case And(Text(var left), Text(var right)): return new Text(left + right);
                case Define(var identifier, var child): Define(child, identifier, build); return true;
                case Refer(var identifier): return new Refer(new Identifier.Index(Index(identifier, build)));
                default: return node;
            }
        }
    }

    readonly Parse _root;
    readonly Parse[] _references;

    Parser(Parse root, Parse[] references)
    {
        _root = root;
        _references = references;
    }

    public Tree[] Parse(string text)
    {
        var state = new State(0, text, new(), new(), new(), _references);
        return _root(ref state) ? state.Trees.Select(pair => pair.tree).ToArray() : Array.Empty<Tree>();
    }
}