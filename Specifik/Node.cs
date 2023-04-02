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
        public override string ToString() => $@"""{Value.Replace("\0", "")}""";
    }

    public sealed record Push(string Value) : Node
    {
        public override string ToString() => $@"{nameof(Push)}(""{Value}"")";
    }

    public sealed record Pop : Node
    {
        public override string ToString() => nameof(Pop);
    }

    public static Node operator ~(Node node) => Option(node);
    public static implicit operator Node(char value) => new Text($"{value}");
    public static implicit operator Node(string value) => new Text(value);
    public static implicit operator Node(bool value) => value ? _true : _false;
    public static implicit operator Node(Range value) => Any(Enumerable
        .Range(value.Start.Value, value.End.Value - value.Start.Value)
        .Select(value => (Node)(char)value)
        .ToArray());

    public static Node Space => Any(Utility.Spaces.Select(value => (Node)value));
    public static Node Spaces => Spawn(nameof(Space), Loop(1, null, Space));
    public static Node Wrap(params Node[] nodes) => All(~Spaces, All(nodes), ~Spaces);

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

    public static Node Loop(int? minimum, int? maximum, IEnumerable<Node> nodes) => Loop(minimum, maximum, nodes.ToArray());
    public static Node Loop(int? minimum, int? maximum, params Node[] nodes)
    {
        if (maximum < minimum || maximum == 0) return _false;
        else if (minimum == 1 && maximum == 1) return All(nodes);

        var identifier = Identifier.Next();
        var refer = new Refer(identifier);
        var define = new Define(identifier, All(nodes));
        var pre = All(Enumerable.Repeat(refer, minimum ?? 0));
        var post = maximum switch
        {
            null => Loop(refer),
            0 => _false,
            1 => refer,
            int value => Chain(Enumerable.Repeat(refer, value))
        };
        return All(define, pre, post);
    }
    public static Node Loop(IEnumerable<Node> nodes) => Loop(nodes.ToArray());
    public static Node Loop(params Node[] nodes)
    {
        var identifier = Identifier.Next();
        var refer = new Refer(identifier);
        var node = Option(All(nodes), refer);
        var define = new Define(identifier, node);
        return All(define, refer);
    }

    public static Node Chain(IEnumerable<Node> nodes) => Chain(nodes.ToArray());
    public static Node Chain(params Node[] nodes) => nodes.Reverse().Aggregate(_true, (sum, node) => Option(node, sum));

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