using static Specifik.Node;

namespace Specifik.Parse.Tests;

public sealed class ParseTests
{
    const string Verb = nameof(Verb), Verb1 = nameof(Verb1), Verb2 = nameof(Verb2), Verb3 = nameof(Verb3);
    const string Option = nameof(Option), Option1 = nameof(Option1), Option2 = nameof(Option2), Option3 = nameof(Option3);
    const string Value = nameof(Value), Value1 = nameof(Value1), Value2 = nameof(Value2), Value3 = nameof(Value3);
    const string Help = nameof(Help);
    const string Version = nameof(Version);

    [Fact]
    public void SingleVerb()
    {
        var node = Spawn("Verb", Wrap("verb"), Wrap("--option"));
        Assert.True(Parser.Parse(node, "verb --option") is [Tree
        {
            Kind: Verb,
            Value.Span: "verb --option",
        }]);
    }

    [Fact]
    public void SingleVerbSurrounded()
    {
        var node = Spawn(Verb, Wrap("verb"), Spawn(Option1, Wrap("--option1")), Wrap("--option"), Spawn(Option2, Wrap("--option2")));
        Assert.True(Parser.Parse(node, "verb --option1 --option --option2") is [Tree
        {
            Kind: Verb,
            Value.Span: "verb --option1 --option --option2",
            Trees:
            [
                _,
                Tree { Kind: Option1, Value.Span: "--option1" },
                _,
                Tree { Kind: Option2, Value.Span: "--option2" }
            ]
        }]);
    }

    [Fact]
    public void SingleVerbWithOptional()
    {
        var node = Spawn(Verb, Wrap("verb"), ~Spawn(Option, Any(Wrap("--option"), Wrap("-o"))));
        // Assert.True(Parser.Parse(node, "verb") is [Tree { Kind: Verb, Values: ["verb"], Trees: [] }]);
        Assert.True(Parser.Parse(node, "verb --option") is [Tree
        {
            Kind: Verb,
            Value.Span: "verb --option",
            Trees: [_, Tree { Kind: Option, Value.Span: "--option" }]
        }]);
        Assert.True(Parser.Parse(node, "verb -o") is [Tree
        {
            Kind: Verb,
            Value.Span: "verb -o",
            Trees: [_, Tree { Kind: Option, Value.Span: "-o" }]
        }]);
    }

    [Fact]
    public void SingleVerbWithSpawn()
    {
        var node = Spawn(Verb, Wrap("verb"), Spawn(Option, Wrap("--option"), Spawn(Value, Loop(33..256))));
        Assert.True(Parser.Parse(node, "verb --option value") is [Tree
        {
            Kind: Verb,
            Trees: [_, Tree
            {
                Kind: Option,
                Trees: [_, Tree { Kind: Value, Value.Span: "value" }]
            }]
        }]);
    }

    [Fact]
    public void MultipleVerbs()
    {
        var node = Any(
            Spawn(Help, Any(Wrap("help"), Wrap("--help"))),
            Spawn(Version, Any(Wrap("version"), Wrap("--version"))),
            Spawn(Verb1,
                Wrap("verb1"),
                Loop(Any(
                    Spawn(Option1, Any(Wrap("--option1"), Wrap("-o1")), Spawn(Value1, Loop(33..256))),
                    Spawn(Help, Any(Wrap("--help"), Wrap("-h"))),
                    Spawn(Version, Any(Wrap("--version"), Wrap("-v")))))),
            Spawn(Verb2, Wrap("verb2"), Loop(Spawn(Option2, Any(Wrap("--option2"), Wrap("-o2"))))),
            Spawn(Verb3, Wrap("verb3"), Loop(Spawn(Option3, Any(Wrap("--option3"), Wrap("-o3"))))));
        Assert.True(Parser.Parse(node, "help") is [Tree { Kind: Help, Value.Span: "help" }]);
        Assert.True(Parser.Parse(node, "--help") is [Tree { Kind: Help, Value.Span: "--help" }]);
        Assert.True(Parser.Parse(node, "verb1 --option1 value1 --help --version") is [Tree
        {
            Kind: Verb1,
            Trees:
            [
                _,
                Tree { Kind: Option1, Trees: [_, Tree { Kind: Value1, Value.Span: "value1" }] },
                Tree { Kind: Help },
                Tree { Kind: Version }
            ]
        }]);
    }
}