using static Specifik.Node;

namespace Specifik.Command;

public sealed record Tree(string Kind, string[] Values, Tree[] Trees);

delegate bool Parse(ref State state);

readonly record struct Build(
    Dictionary<Identifier, int> Indices,
    Dictionary<Node, int> References,
    List<Node?> Nodes);

record struct State(
    int Argument,
    int Index,
    Stack<(int argument, int index, string kind)> Stack,
    Stack<(Tree tree, int depth)> Trees,
    string[] Arguments,
    Parse[] References);

public sealed class Command
{
    public string? Version { get; init; }
    public string? Help { get; init; }
    public Verb[] Verbs { get; init; } = Array.Empty<Verb>();
}

public sealed class Verb
{
    public required string Name { get; init; }
    public required string[] Names { get; init; } = Array.Empty<string>();
    public string? Help { get; init; }
    public Option[] Options { get; init; } = Array.Empty<Option>();

    // public Node Node() =>
    //     string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException(nameof(Name)) :
    //     Child(Name, Any(Name, Any(Names.Select(name => (Node)name))));oifus
}

public sealed class Option
{
    public required string Name { get; init; }
    public string? Short { get; init; }

    // public Node Node() =>
    //     string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException(nameof(Name)) :
    //     Child(Name, string.IsNullOrWhiteSpace(Short) ? $"--{Name}" : Any($"--{Name}", $"-{Short}"), Child("Value", Loop(32..256)));
}

public sealed class Parser
{
    public static Parser From(Node node)
    {
        var build = new Build(new(), new(), new());
        node = node.Descend(node => node, node => Identify(node, build));
        var references = new Parse?[build.Nodes.Count];
        for (int i = 0; i < build.Nodes.Count; i++) references[i] = Next(build.Nodes[i]!, references);
        var root = Next(node, references);
        return new(root, references!);

        static Parse Next(Node node, Parse?[] references)
        {
            switch (node)
            {
                case True: return (ref State _) => true;
                case False: return (ref State _) => false;
                case And and:
                    var all = and.Flatten().Select(node => Next(node, references)).ToArray();
                    return (ref State state) =>
                    {
                        foreach (var parser in all) if (parser(ref state) == false) return false;
                        return true;
                    };
                case Or or:
                    var any = or.Flatten().Select(node => Next(node, references)).ToArray();
                    return (ref State state) =>
                    {
                        foreach (var parser in any)
                        {
                            var local = state with
                            {
                                Stack = new(state.Stack),
                                Trees = new(state.Trees)
                            };
                            if (parser(ref local))
                            {
                                state = local;
                                return true;
                            }
                        }
                        return false;
                    };
                case Refer(Identifier.Index(var index)): return (ref State state) => state.References[index](ref state);
                case Push(var kind):
                    return (ref State state) =>
                    {
                        state.Stack.Push((state.Argument, state.Index, kind));
                        return true;
                    };
                case Pop:
                    return (ref State state) =>
                    {
                        var depth = state.Stack.Count;
                        if (state.Stack.TryPop(out var frame))
                        {
                            var trees = new List<Tree>();
                            while (state.Trees.TryPop(out var pair))
                            {
                                if (pair.depth > depth) trees.Add(pair.tree);
                                else { state.Trees.Push(pair); break; }
                            }
                            trees.Reverse();

                            var values = new List<string>();
                            for (int i = frame.argument; i <= state.Argument && i < state.Arguments.Length; i++)
                            {
                                var argument = state.Arguments[i];
                                var value = i == state.Argument ? argument[frame.index..state.Index] : argument[frame.index..];
                                if (value.Length > 0) values.Add(value);
                                frame.index = 0;
                            }
                            state.Trees.Push((new Tree(frame.kind, values.ToArray(), trees.ToArray()), depth));
                            return true;
                        }
                        else return false;
                    };
                case Text(var text):
                    return (ref State state) =>
                    {
                        if (state.Argument < state.Arguments.Length &&
                            state.Arguments[state.Argument] is var argument &&
                            argument.AsSpan(state.Index).Trim().StartsWith(text))
                        {
                            state.Index += text.Length;
                            if (state.Index == argument.Length)
                            {
                                state.Index = 0;
                                state.Argument += 1;
                            }
                            return true;
                        }
                        return false;
                    };
                default: throw new NotImplementedException();
            }
        }

        static int Define(Node node, Identifier identifier, Build build)
        {
            if (build.References.TryGetValue(node, out var index)) return index;
            index = Index(identifier, build);
            build.Nodes[index] = node;
            build.References[node] = index;
            return index;
        }

        static Node Identify(Node node, Build build)
        {
            switch (node.Boolean())
            {
                case Define(var identifier, var child): Define(child, identifier, build); return true;
                case Refer(var identifier): return new Refer(new Identifier.Index(Index(identifier, build)));
                default: return node;
            }
        }

        static int Index(Identifier identifier, Build build)
        {
            if (identifier is Identifier.Index(var index)) return index;
            if (build.Indices.TryGetValue(identifier, out index)) return index;
            index = build.Indices.Count;
            build.Nodes.Add(null);
            build.Indices.Add(identifier, index);
            return index;
        }
    }

    readonly Parse _parse;
    readonly Parse[] _references;

    Parser(Parse root, Parse[] references)
    {
        _parse = root;
        _references = references;
    }

    public Tree[] Parse(params string[] arguments)
    {
        var state = new State()
        {
            Arguments = arguments,
            Stack = new(),
            References = _references,
            Trees = new(),
        };
        if (_parse(ref state) && state.Argument == state.Arguments.Length)
            return state.Trees.Select(pair => pair.tree).ToArray();
        else
            return Array.Empty<Tree>();
    }
}