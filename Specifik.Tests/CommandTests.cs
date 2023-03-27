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
        Assert.True(Parser.Parse(node, "verb", "--option") is [Tree
        {
            Kind: Verb,
            Values: ["verb", "--option"],
            Trees: []
        }]);
    }

    [Fact]
    public void SingleVerbSurrounded()
    {
        var node = Spawn(Verb, "verb", Spawn(Option1, "--option1"), "--option", Spawn(Option2, "--option2"));
        Assert.True(Parser.Parse(node, "verb", "--option1", "--option", "--option2") is [Tree
        {
            Kind: Verb,
            Values: ["verb", "--option1", "--option", "--option2"],
            Trees:
            [
                Tree { Kind: Option1, Values: ["--option1"], Trees: [] },
                Tree { Kind: Option2, Values: ["--option2"], Trees: [] }
            ]
        }]);
    }

    [Fact]
    public void SingleVerbWithOptional()
    {
        var node = Spawn(Verb, "verb", ~Spawn(Option, Any("--option", "-o")));
        // Assert.True(Parser.Parse(node, "verb") is [Tree { Kind: Verb, Values: ["verb"], Trees: [] }]);
        Assert.True(Parser.Parse(node, "verb", "--option") is [Tree
        {
            Kind: Verb,
            Values: ["verb", "--option"],
            Trees: [Tree { Kind: Option, Values: ["--option"], Trees: [] }]
        }]);
        Assert.True(Parser.Parse(node, "verb", "-o") is [Tree
        {
            Kind: Verb,
            Values: ["verb", "-o"],
            Trees: [Tree { Kind: Option, Values: ["-o"], Trees: [] }]
        }]);
    }

    [Fact]
    public void SingleVerbWithSpawn()
    {
        var node = Spawn(Verb, "verb", Spawn(Option, "--option", Spawn(Value, Loop(33..256))));
        var parser = Parser.From(node);
        Assert.True(parser.Parse("verb", "--option", "value") is [Tree
        {
            Kind: Verb,
            Trees: [Tree
            {
                Kind: Option,
                Trees: [Tree { Kind: Value, Values: ["value"], Trees: [] }]
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
                ~Spawn(Option1, Any("--option1", "-o1"), Spawn(Value1, Loop(33..255))),
                ~Spawn(Help, Any("--help", "-h")),
                ~Spawn(Version, Any("--version", "-v"))),
            Spawn(Verb2, "verb2", ~Spawn(Option2, Any("--option2", "-o2"))),
            Spawn(Verb3, "verb3", ~Spawn(Option3, Any("--option3", "-o3"))));
        var parser = Parser.From(node);
        Assert.True(parser.Parse("help") is [Tree { Kind: Help, Values: ["help"], Trees: [] }]);
        Assert.True(parser.Parse("--help") is [Tree { Kind: Help, Values: ["--help"], Trees: [] }]);
        Assert.True(parser.Parse("verb1", "--option1", "value1", "--help", "--version") is [Tree
        {
            Kind: Verb1,
            Trees:
            [
                Tree { Kind: Option1, Trees: [Tree { Kind: Value1, Values: ["value1"] }] },
                Tree { Kind: Help },
                Tree { Kind: Version }
            ]
        }]);
    }
}