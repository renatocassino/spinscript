namespace SpinScript.Tests.src.Examples;

using SpinScript.Analysis;
using SpinScript.Compiler;
using Xunit;

/// <summary>
/// The playground's "Examples" menu loads spinscript-web/src/examples/*.spin
/// as they are, so each one has to compile with no errors and no warnings —
/// an example that greets people with a red box defeats the point.
/// </summary>
public class PlaygroundExamplesTests
{
    public static TheoryData<string> Examples()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.GetFiles(ExamplesDirectory(), "*.spin").Order())
        {
            data.Add(Path.GetFileName(file));
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public void ExampleCompilesWithoutDiagnostics(string fileName)
    {
        var source = File.ReadAllText(Path.Combine(ExamplesDirectory(), fileName));

        Assert.Empty(SemanticAnalyzer.Check(source));

        var compiler = new Compiler(source);
        var result = compiler.Compile();
        Assert.NotEmpty(result.Events);
    }

    [Fact]
    public void ThereAreExamples()
    {
        Assert.NotEmpty(Examples());
    }

    private static string ExamplesDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "spinscript.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."),
            "spinscript-web", "src", "examples");
    }
}
