using static Specifik.Node;

namespace Specifik.Command.Tests;

public sealed class CommandTests
{
    const string Verb = nameof(Verb), Verb1 = nameof(Verb1), Verb2 = nameof(Verb2), Verb3 = nameof(Verb3);
    const string Option = nameof(Option), Option1 = nameof(Option1), Option2 = nameof(Option2), Option3 = nameof(Option3);
    const string Value = nameof(Value), Value1 = nameof(Value1), Value2 = nameof(Value2), Value3 = nameof(Value3);
    const string Help = nameof(Help);
    const string Version = nameof(Version);

    [Fact]
    public void SingleVerb()
    {
        var node = Spawn(Verb, "verb", "--option");
        var parser = Parser.From(node);
        Assert.True(parser.Parse("verb", "--option") is [Tree
        {
            Kind: Verb1,
            Values: ["--option"],
            Trees: []
        }]);
    }

    [Fact]
    public void SingleVerbSurrounded()
    {
        var node = Spawn(Verb, "verb", Spawn(Option1, "--option1"), "--option2", Spawn(Option3, "--option3"));
        var parser = Parser.From(node);
        Assert.True(parser.Parse("verb", "--option1", "--option2", "--option3") is [Tree
        {
            Kind: Verb1,
            Values: [_, "--option2", _],
            Trees:
            [
                Tree { Kind: Option1, Values: ["--option1"], Trees: [] },
                Tree { Kind: Option3, Values: ["--option3"], Trees: [] }
            ]
        }]);
    }

    [Fact]
    public void SingleVerbWithOptional()
    {
        var node = Spawn(Verb1, "verb", ~Spawn(Option1, Any("--option", "-o")));
        var parser = Parser.From(node);
        Assert.True(parser.Parse("verb") is [Tree { Kind: Verb1, Values: [], Trees: [] }]);
        Assert.True(parser.Parse("verb", "--option") is [Tree
        {
            Kind: Verb1,
            Values: [],
            Trees: [Tree { Kind: Option1, Values: [], Trees: [] }]
        }]);
        Assert.True(parser.Parse("verb", "-o") is [Tree
        {
            Kind: Verb1,
            Values: [],
            Trees: [Tree { Kind: Option1, Values: [], Trees: [] }]
        }]);
    }

    [Fact]
    public void SingleVerbWithStore()
    {
        var node = Spawn(Verb1, "verb", Spawn(Option1, "--option", Spawn(Value1, Loop(33..256))));
        var parser = Parser.From(node);
        Assert.True(parser.Parse("verb", "--option", "value") is [Tree
        {
            Kind: Verb1,
            Trees: [Tree
            {
                Kind: Option1,
                Trees: [Tree { Kind: Value1, Values: ["value"], Trees: [] }]
            }]
        }]);
    }

    [Fact]
    public void MultipleVerbs()
    {
        var node = Any(
            Spawn(Help, Any("help", "--help")),
            Spawn(Version, Any("version", "--version")),
            Spawn(Verb1,
                "verb1",
                ~Spawn(Option1, Any("--option", "-o"), Spawn(Value1, Loop(33..255))),
                ~Spawn(Help, Any("--help", "-h")),
                ~Spawn(Version, Any("--version", "-v"))),
            Spawn(Verb2, "verb2", ~Spawn(Option2, Any("--option", "-o"))),
            Spawn(Verb3, "verb3", ~Spawn(Option3, Any("--option", "-o"))));
        var parser = Parser.From(node);
        Assert.True(parser.Parse("help") is [Tree { Kind: Help, Values: [], Trees: [] }]);
        Assert.True(parser.Parse("--help") is [Tree { Kind: Help, Values: [], Trees: [] }]);
        Assert.True(parser.Parse("verb1", "--option", "value", "--help") is [Tree
        {
            Kind: Verb1,
            Values: [],
            Trees: [Tree
            {
                Kind: Option1,
                Values: ["value"],
                Trees: [Tree { Kind: Help, Values: [], Trees: [] }]
            }]
        } t]);
    }
}