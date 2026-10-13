namespace SpinScript.Tests.src.Analysis;

using SpinScript.Analysis;
using SpinScript.Compiler;
using Xunit;

public class SemanticAnalyzerTests
{
    // Minimal valid file every test builds on: one sampled beat, played.
    private const string Valid = """
@bpm = 120;
@kick = "kick.wav";

beat @beat (sample=@kick, grid=16) { 1, 5, 9, 13 };

loop @groove (bars=1) {
    play @beat;
}

song {
    play @groove;
}
""";

    private static IReadOnlyList<Diagnostic> Check(string source) => SemanticAnalyzer.Check(source);

    private static Diagnostic Single(string source, string code)
    {
        var matches = Check(source).Where(d => d.Code == code).ToList();
        return Assert.Single(matches);
    }

    [Fact]
    public void ValidProgramHasNoDiagnostics()
    {
        Assert.Empty(Check(Valid));
    }

    [Fact]
    public void PlayOfUndeclaredNameIsAnErrorWithPositionAndSuggestion()
    {
        var source = Valid.Replace("play @beat;", "play @beet;");

        var diagnostic = Single(source, "unknown-reference");

        Assert.True(diagnostic.IsError);
        Assert.Equal(6, diagnostic.Line);
        Assert.Equal(9, diagnostic.Column);
        Assert.Contains("Did you mean '@beat'?", diagnostic.Message);
    }

    [Fact]
    public void PlayOfAVariableIsAnError()
    {
        var diagnostic = Single(Valid.Replace("play @beat;", "play @kick;"), "unknown-reference");

        Assert.Contains("is a variable", diagnostic.Message);
    }

    [Fact]
    public void PlayOfALoopDeclaredInlineIsAnError()
    {
        var source = Valid.Replace("""
loop @groove (bars=1) {
    play @beat;
}
""", """
loop @groove (bars=1) {
    loop @inner { play @beat; }
    play @inner;
}
""");

        var diagnostic = Single(source, "unknown-reference");

        Assert.Contains("declared inside a loop", diagnostic.Message);
    }

    [Fact]
    public void ParameterReferencingUndefinedVariableIsAnError()
    {
        var diagnostic = Single(Valid.Replace("sample=@kick", "sample=@kik"), "unknown-reference");

        Assert.True(diagnostic.IsError);
        Assert.Contains("Did you mean '@kick'?", diagnostic.Message);
    }

    [Theory]
    [InlineData("grid=\"abc\"")]
    [InlineData("grid=0")]
    [InlineData("grid=2.5")]
    public void InvalidGridIsAnError(string grid)
    {
        var diagnostic = Single(Valid.Replace("grid=16", grid), "invalid-parameter");

        Assert.True(diagnostic.IsError);
        Assert.Contains("'grid'", diagnostic.Message);
        Assert.DoesNotContain("NumberValue", diagnostic.Message);
        Assert.DoesNotContain("StringValue", diagnostic.Message);
    }

    [Fact]
    public void ParameterTypeIsCheckedThroughTheReferencedVariable()
    {
        var source = Valid.Replace("@kick = \"kick.wav\";", "@kick = \"kick.wav\";\n@size = \"big\";")
            .Replace("grid=16", "grid=@size");

        Assert.Single(Check(source), d => d.Code == "invalid-parameter");
    }

    [Fact]
    public void InvalidRepeatAndBarsAreErrors()
    {
        var source = Valid
            .Replace("loop @groove (bars=1)", "loop @groove (bars=0)")
            .Replace("play @groove;", "play @groove (repeat=\"2\");");

        Assert.Equal(2, Check(source).Count(d => d.Code == "invalid-parameter"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("17")]
    public void StepOutsideTheGridIsAnError(string step)
    {
        var diagnostic = Single(Valid.Replace("{ 1, 5, 9, 13 }", $"{{ 1, {step} }}"), "step-out-of-grid");

        Assert.Contains($"Step {step}", diagnostic.Message);
    }

    [Fact]
    public void StepRangeFollowsTheBeatsGrid()
    {
        Assert.Empty(Check(Valid.Replace("grid=16", "grid=32").Replace("{ 1, 5, 9, 13 }", "{ 1, 32 }")));
    }

    [Fact]
    public void PatternGridLongerThanTheGridIsAnError()
    {
        Assert.Single(Check(Valid.Replace("grid=16", "grid=4").Replace("{ 1, 5, 9, 13 }", "{ x...|x... }")),
            d => d.Code == "step-out-of-grid");
    }

    [Fact]
    public void DuplicateStepIsAWarning()
    {
        var diagnostic = Single(Valid.Replace("{ 1, 5, 9, 13 }", "{ 1, 5, 5 }"), "duplicate-step");

        Assert.False(diagnostic.IsError);
    }

    private const string WithMelody = """
@piano = "piano.wav";

melody @lead (sample=@piano) { NOTES };

song {
    play @lead;
}
""";

    [Fact]
    public void NoteWithoutOctaveIsAnError()
    {
        var diagnostic = Single(WithMelody.Replace("NOTES", "C4 0 1/4, A 1/4 1/4"), "invalid-note");

        Assert.Equal(2, diagnostic.Line);
        Assert.Equal(41, diagnostic.Column);
    }

    [Fact]
    public void ZeroDurationNoteIsAWarning()
    {
        var diagnostic = Single(WithMelody.Replace("NOTES", "C4 0 0"), "silent-note");

        Assert.False(diagnostic.IsError);
    }

    [Fact]
    public void DivisionByZeroInNoteTimeIsAnError()
    {
        Assert.Single(Check(WithMelody.Replace("NOTES", "C4 1/0 1/4")), d => d.Code == "invalid-time");
    }

    [Fact]
    public void InvalidRootNoteIsAnError()
    {
        Assert.Single(Check(WithMelody.Replace("NOTES", "C4 0 1/4").Replace("(sample=@piano)", "(sample=@piano, rootNote=\"C\")")),
            d => d.Code == "invalid-parameter");
    }

    [Fact]
    public void DirectCycleIsAnError()
    {
        var source = Valid.Replace("    play @beat;\n}", "    play @beat;\n    play @groove;\n}");

        var diagnostic = Single(source, "cycle");

        Assert.Contains("@groove → @groove", diagnostic.Message);
    }

    [Fact]
    public void IndirectCycleThroughNestedLoopIsReportedOnce()
    {
        var source = """
loop @a { play @b; }
loop @b { loop @inner { play @a; } }

song {
    play @a;
}
""";

        var diagnostic = Single(source, "cycle");

        Assert.Contains("@a → @b → @a", diagnostic.Message);
        Assert.Equal(1, diagnostic.Line);
    }

    [Fact]
    public void MissingSongIsAnError()
    {
        Assert.Single(Check("@bpm = 120;"), d => d.Code == "missing-song");
    }

    [Fact]
    public void SecondSongIsAnError()
    {
        var diagnostic = Single(Valid + "\nsong { play @groove; }", "duplicate-song");

        Assert.Equal(12, diagnostic.Line);
    }

    [Fact]
    public void PlayAtTopLevelIsAnError()
    {
        Assert.Single(Check(Valid + "\nplay @beat;"), d => d.Code == "play-outside-loop");
    }

    [Fact]
    public void DuplicateDeclarationIsAnError()
    {
        var diagnostic = Single(Valid + "\nloop @beat { }", "duplicate-declaration");

        Assert.Contains("already declared as a beat on line 4", diagnostic.Message);
    }

    [Theory]
    [InlineData("@bpm = 0;")]
    [InlineData("@bpm = \"fast\";")]
    [InlineData("@beatsPerBar = 3.5;")]
    public void InvalidConfigurationIsAnError(string assignment)
    {
        var diagnostic = Single(Valid.Replace("@bpm = 120;", assignment), "invalid-config");

        Assert.True(diagnostic.IsError);
    }

    [Fact]
    public void UnplayedDeclarationIsAWarning()
    {
        var diagnostic = Single(Valid + "\nbeat @extra (sample=@kick) { 1 }", "unused-declaration");

        Assert.False(diagnostic.IsError);
        Assert.Contains("'@extra'", diagnostic.Message);
    }

    [Fact]
    public void UnusedVariableIsAWarningButConfigIsNot()
    {
        var diagnostic = Single(Valid + "\n@unused = 1;", "unused-variable");

        Assert.Contains("'@unused'", diagnostic.Message);
    }

    [Fact]
    public void UnknownParameterIsAWarningWithSuggestion()
    {
        var diagnostic = Single(Valid.Replace("grid=16", "grdi=16"), "unknown-parameter");

        Assert.False(diagnostic.IsError);
        Assert.Contains("Did you mean 'grid'?", diagnostic.Message);
    }

    [Fact]
    public void InstrumentParameterSuggestsSample()
    {
        var source = WithMelody.Replace("NOTES", "C4 0 1/4").Replace("sample=@piano", "instrument=@piano");

        Assert.Contains(Check(source), d => d.Code == "unknown-parameter" && d.Message.Contains("Did you mean 'sample'?"));
        Assert.Contains(Check(source), d => d.Code == "missing-sample");
    }

    [Fact]
    public void AssignmentInsideABlockIsAWarning()
    {
        Assert.Single(Check(Valid.Replace("    play @groove;", "    @volume = 80;\n    play @groove;")),
            d => d.Code == "ignored-assignment");
    }

    [Fact]
    public void ReportsEveryProblemInOnePassSortedByPosition()
    {
        var source = """
beat @kick (grid=4) { 9 };

song {
    play @missing;
}
""";

        var diagnostics = Check(source);

        Assert.Equal(["missing-sample", "step-out-of-grid", "unused-declaration", "unknown-reference"],
            diagnostics.Select(d => d.Code).Distinct());
        Assert.Equal(diagnostics.OrderBy(d => d.Line).ThenBy(d => d.Column), diagnostics);
    }

    [Fact]
    public void SyntaxErrorsAreReturnedWithoutSemanticNoise()
    {
        var diagnostic = Assert.Single(Check("@bpm = ;\nsong { play @nothing; }"));

        Assert.Equal("syntax-error", diagnostic.Code);
    }

    [Fact]
    public void CompilerRejectsSemanticErrorsWithAllDiagnostics()
    {
        var compiler = new Compiler(Valid.Replace("play @beat;", "play @nope;\n    play @nada;"));

        var exception = Assert.Throws<CompilerException>(compiler.Compile);

        Assert.Equal(2, exception.Errors.Count);
        Assert.All(exception.Errors, error => Assert.Equal("unknown-reference", error.Code));
    }

    [Fact]
    public void CompilerExposesWarningsOnSuccess()
    {
        var compiler = new Compiler(Valid + "\n@unused = 1;");

        compiler.Compile();

        Assert.Single(compiler.Diagnostics, d => d.Code == "unused-variable");
        Assert.NotEmpty(compiler.compileResult.Events);
    }

    [Fact]
    public void CompilerResolvesReferencesInAnyParameter()
    {
        var source = Valid.Replace("@kick = \"kick.wav\";", "@kick = \"kick.wav\";\n@steps = 4;")
            .Replace("grid=16", "grid=@steps")
            .Replace("{ 1, 5, 9, 13 }", "{ 1, 3 }");
        var compiler = new Compiler(source);

        compiler.Compile();

        // grid=4 at 120 BPM: 1 step = 500ms
        Assert.Equal([0.0, 1000.0], compiler.compileResult.Events.Select(e => ((SoundEvent)e).Time));
    }
}
