using System.Text;
using static Specifik.Node;

namespace Specifik.Generate;

record struct State(StringBuilder Builder, Random Random, Generate[] References);

delegate bool Generate(ref State state);

public sealed class Generator
{
    public static Generator From(Node node)
    {
        var nodes = new List<Node?>();
        var references = new Generate?[nodes.Count];
        for (int i = 0; i < nodes.Count; i++) references[i] = Next(nodes[i]!);
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

    Generate _root;
    Generate[] _references;

    Generator(Generate root, Generate[] references)
    {
        _root = root;
        _references = references;
    }

    public string[] Generate()
    {
        var state = new State(new(), new(), _references);
        if (_root(ref state)) return state.Builder.ToString().Split('\0', StringSplitOptions.RemoveEmptyEntries);
        else return Array.Empty<string>();
    }
}