using Specifik.Command;
using Specifik.Parse;

namespace Specifik.Generate;

public sealed class GenerateTests
{
    record Verb1;
    record Verb2;

    [Fact]
    public void Boba()
    {
        var node = new Command.Command()
        {
            Verbs = new Verb[]
            {
                new Verb<Verb1>
                {
                    Options = new Option[]
                    {
                        new Option<double> { Name = "option1" },
                        new Option<string> { Name = "option2", Shorts = new[] { "o2" } }
                    }
                },
                new Verb<Verb2>
                {
                    Options = new Option[]
                    {
                        new Option<int> { Name = "option1" },
                        new Option<byte> { Name = "option2", Shorts = new[] { "o2" } }
                    }
                }
            }
        }.Node();
        var parser = Parser.From(node);
        var generator = Generator.From(node);
        var a = Enumerable.Repeat(0, 100).Select(_ => generator.Generate()).ToArray();
        var b = a.Select(text => text is null ? null : parser.Parse(text)).ToArray();
    }
}