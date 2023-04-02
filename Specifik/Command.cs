using Specifik.Parse;
using static Specifik.Node;

namespace Specifik.Command;

public sealed record Error(string Message);
public sealed record Help;
public sealed record Version;
public sealed record Excess;

public sealed class Command
{
    public static implicit operator Node(Command command) => command.Node();
    public string? Version { get; init; }
    public string? Help { get; init; }
    public Verb[] Verbs { get; init; } = Array.Empty<Verb>();

    public Node Node() => Any(Verbs
        .Append(new Verb<Help>())
        .Append(new Verb<Version>())
        .Select(verb => verb.Node())
        .Append(Spawn(nameof(Excess), Loop(1, null, 0..256))));

    public object Parse(params string[] arguments) => Convert(Parser.Parse(Node(), string.Join(" ", arguments)));

    public object Convert(params Tree[] trees)
    {
        switch (trees)
        {
            case []: return new Error("No match.");
            case [var tree]:
                foreach (var verb in Verbs)
                {
                    var result = verb.Convert(tree);
                    if (result is Error) continue;
                    else return result;
                }
                return new Error("No match.");
            default: return new Error("Ambiguous match.");
        }
    }
}

public abstract class Verb
{
    public static implicit operator Node(Verb verb) => verb.Node();
    public string[] Names { get; init; } = Array.Empty<string>();
    public string? Help { get; init; }
    public Option[] Options { get; init; } = Array.Empty<Option>();
    public abstract Node Node();
    public abstract object Convert(Tree tree);
}

public sealed class Verb<T> : Verb where T : new()
{
    public override Node Node() => Spawn(typeof(T).Name,
        Any(Wrap(typeof(T).Name), Any(Names.Select(name => Wrap(name)))),
        Loop(Any(Options.Select(option => option.Node()))));

    public override object Convert(Tree tree)
    {
        if (tree.Kind == typeof(T).Name)
        {
            var instance = new T();
            foreach (var child in tree.Trees)
            {
                // child.k
            }
            return instance;
        }
        else return new Error("Wrong kind.");
    }
}

public abstract class Option
{
    public static implicit operator Node(Option option) => option.Node();
    public required string Name { get; init; }
    public string[] Names { get; init; } = Array.Empty<string>();
    public string[] Shorts { get; init; } = Array.Empty<string>();
    public abstract Node Node();
}

public sealed class Option<T> : Option
{
    public override Node Node() => Spawn(Name,
        Any(Wrap($"--{Name}"), Any(Names.Select(name => Wrap($"--{name}"))), Any(Shorts.Select(name => Wrap($"-{name}")))),
        Spawn(typeof(T).Name, Loop(1, null, 33..256)));
}