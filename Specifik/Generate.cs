using System.Text;
using static Specifik.Node;

namespace Specifik.Generate;

record struct State(StringBuilder Builder, Random Random, Generate[] References);

readonly record struct Build(Dictionary<Identifier, int> Indices, List<Node?> Nodes);

delegate bool Generate(ref State state);

public sealed class Generator
{
    public static string? Generate(Node node) => From(node).Generate();

    public static Generator From(Node node)
    {
        var build = new Build(new(), new());
        node = node.Descend(_ => _, node => Normalize(node, build));
        var references = new Generate?[build.Nodes.Count];
        for (int i = 0; i < build.Nodes.Count; i++) references[i] = Next(build.Nodes[i]!);
        return new(Next(node), references!);

        static Generate Next(Node node)
        {
            switch (node)
            {
                case True: return (ref State _) => true;
                case False: return (ref State _) => false;
                case And and:
                    var all = and.Flatten().Select(Next).ToArray();
                    return (ref State state) =>
                    {
                        foreach (var generate in all) if (!generate(ref state)) return false;
                        return true;
                    };
                case Or or:
                    var any = or.Flatten().Select(Next).ToArray();
                    return (ref State state) =>
                    {
                        any.Shuffle(state.Random);
                        foreach (var generate in any)
                        {
                            var builder = state.Builder.Length;
                            if (generate(ref state)) return true;
                            state.Builder.Length = builder;
                        }
                        return false;
                    };
                case Refer(Identifier.Index(var index)): return (ref State state) => state.References[index](ref state);
                case Push: return Next(true);
                case Pop: return Next(true);
                case Text(var text):
                    return (ref State state) =>
                    {
                        state.Builder.Append(text);
                        return true;
                    };
                default: throw new NotImplementedException();
            }
        }
    }

    static int Define(Node node, Identifier identifier, Build build)
    {
        var index = Index(identifier, build);
        build.Nodes[index] = node;
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

    static int Index(Identifier identifier, Build build)
    {
        if (identifier is Identifier.Index(var index)) return index;
        if (build.Indices.TryGetValue(identifier, out index)) return index;
        index = build.Indices.Count;
        build.Nodes.Add(null);
        build.Indices.Add(identifier, index);
        return index;
    }

    readonly Generate _root;
    readonly Generate[] _references;

    Generator(Generate root, Generate[] references)
    {
        _root = root;
        _references = references;
    }

    public string? Generate()
    {
        var state = new State(new(), new(), _references);
        if (_root(ref state)) return state.Builder.ToString();
        else return null;
    }
}