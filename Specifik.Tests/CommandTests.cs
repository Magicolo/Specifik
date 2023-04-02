using Specifik.Generate;

namespace Specifik.Command.Tests;

public sealed class CommandTests
{
    record Verb1;
    record Verb2;

    [Fact]
    public void SingleVerb()
    {
        var option1 = new Option<double> { Name = "option1" };
        var option2 = new Option<string> { Name = "option2", Shorts = new[] { "o2" } };
        var verb1 = new Verb<Verb1> { Options = new Option[] { option1, option2 } };
        var verb2 = new Verb<Verb2> { Options = new Option[] { option1, option2 } };
        var command = new Command() { Verbs = new Verb[] { verb1, verb2 } };
        var generator1 = Generator.From(verb1);
        var generator2 = Generator.From(verb2);
        var a = Enumerable.Range(0, 100).Select(_ => generator1.Generate()).ToArray();
        var b = Enumerable.Range(0, 100).Select(_ => generator2.Generate()).ToArray();
    }
}