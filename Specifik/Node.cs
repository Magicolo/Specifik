namespace Specifik;

public abstract record Node
{
    public sealed record True : Node
    {
        public override string ToString() => nameof(True);
    }
    public sealed record False : Node
    {
        public override string ToString() => nameof(False);
    }
    public sealed record And(Node Left, Node Right) : Node
    {
        public IEnumerable<Node> Flatten()
        {
            static IEnumerable<Node> Next(Node node)
            {
                switch (node)
                {
                    case And and:
                        foreach (var child in Next(and.Left)) yield return child;
                        foreach (var child in Next(and.Right)) yield return child;
                        break;
                    default: yield return node; break;
                }
            }
            return Next(this);
        }

        public override string ToString() => $"({string.Join(" & ", Flatten())})";
    }
    public sealed record Or(Node Left, Node Right) : Node
    {
        public IEnumerable<Node> Flatten()
        {
            static IEnumerable<Node> Next(Node node)
            {
                switch (node)
                {
                    case Or or:
                        foreach (var child in Next(or.Left)) yield return child;
                        foreach (var child in Next(or.Right)) yield return child;
                        break;
                    default: yield return node; break;
                }
            }
            return Next(this);
        }

        public override string ToString() => $"({string.Join(" | ", Flatten())})";
    }
    public sealed record Define(Identifier Identifier, Node Node) : Node
    {
        public override string ToString() => $@"({Identifier} = {Node})";
    }
    public sealed record Refer(Identifier Identifier) : Node
    {
        public override string ToString() => $@"&{Identifier}";
    }
    public sealed record Text(string Value) : Node
    {
        public override string ToString() => $@"""{Value}""";
    }
    public sealed record Push(string Value) : Node
    {
        public override string ToString() => $@"{nameof(Push)}(""{Value}"")";
    }
    public sealed record Pop : Node
    {
        public override string ToString() => nameof(Pop);
    }

    public static implicit operator Node(bool value) => value ? _true : _false;
    public static implicit operator Node(char value) => new Text($"{value}");
    public static implicit operator Node(string value) => new Text($"\0{value}\0");
    public static implicit operator Node(Range value) => Any(Enumerable
        .Range(value.Start.Value, value.End.Value - value.Start.Value)
        .Select(value => new Text($"{(char)value}"))
        .ToArray());
    public static Node operator ~(Node node) => Option(node);

    public static Node All(IEnumerable<Node> nodes) => All(nodes.ToArray());
    public static Node All(params Node[] nodes) => nodes switch
    {
        [] => _true,
        [var node] => node,
        [var head, .. var tail] => tail.Aggregate(head, (left, right) => new And(left, right)),
    };

    public static Node Any(IEnumerable<Node> nodes) => Any(nodes.ToArray());
    public static Node Any(params Node[] nodes) => nodes switch
    {
        [] => _false,
        [var node] => node,
        [var head, .. var tail] => tail.Aggregate(head, (left, right) => new Or(left, right)),
    };

    public static Node Option(IEnumerable<Node> nodes) => Option(nodes.ToArray());
    public static Node Option(params Node[] nodes) => Any(All(nodes), _true);

    public static Node Loop(IEnumerable<Node> nodes) => Loop(nodes.ToArray());
    public static Node Loop(params Node[] nodes)
    {
        var identifier = Identifier.Next();
        var refer = new Refer(identifier);
        var child = Option(All(nodes), refer);
        var define = new Define(identifier, child);
        return new And(define, refer);
    }

    public static Node Spawn(string kind, IEnumerable<Node> nodes) => Spawn(kind, nodes.ToArray());
    public static Node Spawn(string kind, params Node[] nodes) => All(new Push(kind), All(nodes), _pop);

    static readonly Node _true = new True();
    static readonly Node _false = new False();
    static readonly Node _pop = new Pop();

    Node() { }

    public Node Map(Func<Node, Node> map) => this switch
    {
        And(var left, var right) => new And(map(left), map(right)),
        Or(var left, var right) => new Or(map(left), map(right)),
        Define(var identifier, var child) => new Define(identifier, map(child)),
        _ => this,
    };

    public Node Descend(Func<Node, Node> down, Func<Node, Node> up) => up(down(this).Map(node => node.Descend(down, up)));

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
    public Node Normalize() => this switch
    {
        And(True, var right) => right,
        And(var left, True) => left,
        And(False, _) => _false,
        And(_, False) => _false,
        Or(var left, var right) when left == right => left,
        Or(var left, False) => left,
        Or(False, var right) => right,
        Or(True, var right) => new Or(right, _true),
        Text("") => _true,
        And(Text(var left), Text(var right)) => new Text(left + right),
        _ => this,
    };
}

public abstract record Identifier
{
    public sealed record Unique(ulong Value) : Identifier;
    public sealed record Index(int Value) : Identifier;
    public sealed record Path(string Value) : Identifier
    {
        public override string ToString() => Value;
    }

    static ulong _counter;
    public static Identifier Next() => new Unique(Interlocked.Increment(ref _counter));
    public static implicit operator Identifier(string path) => new Path(path);

    Identifier() { }
}