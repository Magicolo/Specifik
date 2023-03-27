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
        var node = Spawn(Verb, Word("verb"), Word("--option"));
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
        var node = Spawn(Verb, Word("verb"), Spawn(Option1, Word("--option1")), Word("--option"), Spawn(Option2, Word("--option2")));
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
        var node = Spawn(Verb, Word("verb"), ~Spawn(Option, Any(Word("--option"), Word("-o"))));
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
        var node = Spawn(Verb, Word("verb"), Spawn(Option, Word("--option"), Spawn(Value, Loop(32..256))));
        Assert.True(Parser.Parse(node, "verb", "--option", "value") is [Tree
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
            Spawn(Help, Any(Word("help"), Word("--help"))),
            Spawn(Version, Any(Word("version"), Word("--version"))),
            Spawn(Verb1,
                Word("verb1"),
                ~Spawn(Option1, Any(Word("--option1"), Word("-o1")), Spawn(Value1, Loop(32..256))),
                ~Spawn(Help, Any(Word("--help"), Word("-h"))),
                ~Spawn(Version, Any(Word("--version"), Word("-v")))),
            Spawn(Verb2, Word("verb2"), ~Spawn(Option2, Any(Word("--option2"), Word("-o2")))),
            Spawn(Verb3, Word("verb3"), ~Spawn(Option3, Any(Word("--option3"), Word("-o3")))));
        Assert.True(Parser.Parse(node, "help") is [Tree { Kind: Help, Values: ["help"], Trees: [] }]);
        Assert.True(Parser.Parse(node, "--help") is [Tree { Kind: Help, Values: ["--help"], Trees: [] }]);
        Assert.True(Parser.Parse(node, "verb1", "--option1", "value1", "--help", "--version") is [Tree
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