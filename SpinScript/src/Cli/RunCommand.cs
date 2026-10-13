namespace SpinScript.Cli;

using Spectre.Console;
using Spectre.Console.Cli;
using SpinScript.Analysis;
using SpinScript.Compiler;
using SpinScript.Lexer;
using SpinScript.Parser;
using SpinScript.Parser.Ast;

public sealed class RunCommand : Command<RunSettings>
{
    protected override int Execute(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        var (source, sourceLabel, ok) = LoadSource(settings.Input!);
        if (!ok)
        {
            return 1;
        }

        AnsiConsole.Write(new Rule($"[bold aqua]SpinScript[/] · {Markup.Escape(sourceLabel)}").LeftJustified());

        ParseResult parsed;
        try
        {
            parsed = new Parser(source).Parse();
        }
        catch (LexerException ex)
        {
            WriteDiagnostics([Diagnostic.FromLexerException(ex)], source);
            return 1;
        }

        if (parsed.HasErrors)
        {
            WriteDiagnostics(parsed.Errors.Select(Diagnostic.FromParserException).ToList(), source);
            return 1;
        }

        var program = parsed.Ast;
        var diagnostics = SemanticAnalyzer.Analyze(program);

        if (settings.ShowAst)
        {
            WriteAst(program, sourceLabel);
        }
        WriteDiagnostics(diagnostics, source);

        if (diagnostics.Any(diagnostic => diagnostic.IsError))
        {
            return 1;
        }

        var compiler = new Compiler(source);
        try
        {
            compiler.Compile();
        }
        catch (Exception ex) when (ex is CompilerException or InvalidOperationException)
        {
            // The analyzer should catch everything first; this is a backstop.
            AnsiConsole.MarkupLine($"[red bold]error[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        var events = compiler.compileResult.Events;
        WriteTimeline(events, compiler.Configuration);

        var length = events.Count == 0 ? 0 : events.Max(EndTime);
        AnsiConsole.MarkupLine(
            $"[green]✓[/] {events.Count} event(s), {FormatTime(length)} long at {compiler.Configuration.BPM} BPM, {diagnostics.Count} warning(s).");
        return 0;
    }

    private static void WriteTimeline(List<Event> events, SongConfiguration configuration)
    {
        if (events.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]The song produces no events.[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("[bold]time[/]");
        table.AddColumn("[bold]bar.beat[/]");
        table.AddColumn("[bold]sample[/]");
        table.AddColumn("[bold]note[/]");
        table.AddColumn(new TableColumn("[bold]length[/]").RightAligned());

        foreach (var @event in events.OrderBy(e => TimeOf(e)))
        {
            var time = TimeOf(@event);
            var (sample, note, length) = @event switch
            {
                MelodyEvent melody => (melody.Sample, $"[cyan]{melody.Note}[/]", $"{melody.Duration:0} ms"),
                SoundEvent sound => (sound.Sample, "", ""),
                _ => ("?", "", ""),
            };

            table.AddRow(
                FormatTime(time),
                $"[grey]{FormatPosition(time, configuration)}[/]",
                $"[yellow]{Markup.Escape(ShortSampleName(sample))}[/]",
                note,
                length);
        }

        AnsiConsole.Write(table);
    }

    private static double TimeOf(Event @event) => @event switch
    {
        SoundEvent sound => sound.Time,
        MelodyEvent melody => melody.Time,
        _ => 0,
    };

    private static double EndTime(Event @event) => @event is MelodyEvent melody
        ? melody.Time + melody.Duration
        : TimeOf(@event);

    // 1-indexed, like a DAW: the very first beat of the song is "1.1".
    private static string FormatPosition(double timeMs, SongConfiguration configuration)
    {
        var bar = (int)(timeMs / configuration.BarDurationMs);
        var beat = (timeMs - bar * configuration.BarDurationMs) / configuration.BeatDurationMs;
        return $"{bar + 1}.{beat + 1:0.##}";
    }

    private static string FormatTime(double timeMs) =>
        TimeSpan.FromMilliseconds(timeMs).ToString(@"m\:ss\.fff", System.Globalization.CultureInfo.InvariantCulture);

    // ".../samples/kick_808.wav?v=2" -> "kick_808.wav"
    private static string ShortSampleName(string sample)
    {
        var path = sample.Split('?', '#')[0];
        var name = path[(path.LastIndexOf('/') + 1)..];
        return name.Length == 0 ? sample : name;
    }

    private static void WriteDiagnostics(IReadOnlyList<Diagnostic> diagnostics, string source)
    {
        if (diagnostics.Count == 0)
        {
            return;
        }

        var lines = source.Split('\n');
        foreach (var diagnostic in diagnostics)
        {
            var (color, label) = diagnostic.IsError ? ("red", "error") : ("yellow", "warning");
            AnsiConsole.MarkupLine(
                $"[{color} bold]{label}[/] [grey]{diagnostic.Line + 1}:{diagnostic.Column + 1}[/] {Markup.Escape(diagnostic.Message)} [grey]({diagnostic.Code})[/]");

            if (diagnostic.Line < lines.Length)
            {
                var text = lines[diagnostic.Line].TrimEnd('\r');
                AnsiConsole.MarkupLine($"  [grey]│[/] {Markup.Escape(text)}");
                AnsiConsole.MarkupLine($"  [grey]│[/] {new string(' ', Math.Min(diagnostic.Column, text.Length))}[{color}]^[/]");
            }
        }

        var errors = diagnostics.Count(diagnostic => diagnostic.IsError);
        if (errors > 0)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] {errors} error(s), {diagnostics.Count - errors} warning(s).");
        }
    }

    private static (string source, string label, bool ok) LoadSource(string input)
    {
        if (input.EndsWith(".spin", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(input))
            {
                AnsiConsole.MarkupLine($"[red]File not found:[/] {Markup.Escape(input)}");
                return (string.Empty, string.Empty, false);
            }

            return (File.ReadAllText(input), Path.GetFileName(input), true);
        }

        return (input, "<string>", true);
    }

    private static void WriteAst(ProgramNode program, string sourceLabel)
    {
        var tree = new Tree($"[bold]{Markup.Escape(sourceLabel)}[/]");

        foreach (var statement in program.Statements)
        {
            AddNode(tree, statement);
        }

        AnsiConsole.Write(tree);
    }

    private static void AddNode(IHasTreeNodes parent, AstNode node)
    {
        switch (node)
        {
            case LoopNode loop:
                var loopNode = parent.AddNode(
                    $"[magenta]loop[/] [yellow]@{Markup.Escape(loop.Name)}[/] " +
                    $"[grey]({loop.Statements.Count} statement(s))[/]");
                foreach (var child in loop.Statements)
                {
                    AddNode(loopNode, child);
                }
                break;

            case SongNode song:
                var songNode = parent.AddNode(
                    $"[magenta]song[/] [grey]({song.Statements.Count} statement(s))[/]");
                foreach (var child in song.Statements)
                {
                    AddNode(songNode, child);
                }
                break;

            case AssignmentNode assignment:
                parent.AddNode(DescribeAssignment(assignment));
                break;

            case BeatNode beat:
                parent.AddNode(DescribeBeat(beat));
                break;

            case PlayNode play:
                parent.AddNode(DescribePlay(play));
                break;

            default:
                parent.AddNode(Markup.Escape(node.ToString() ?? node.GetType().Name));
                break;
        }
    }

    private static string DescribeAssignment(AssignmentNode assignment) =>
        $"[yellow]@{Markup.Escape(assignment.Name)}[/] [grey]=[/] {DescribeValue(assignment.Value)}";

    private static string DescribeValue(SpinValue value) => value switch
    {
        SpinValue.StringValue s => $"[green]\"{Markup.Escape(s.Value)}\"[/]",
        SpinValue.NumberValue n => $"[cyan]{n.Value}[/]",
        SpinValue.BooleanValue b => $"[magenta]{(b.Value ? "true" : "false")}[/]",
        _ => Markup.Escape(value.ToString() ?? value.GetType().Name),
    };

    private static string DescribeBeat(BeatNode beat)
    {
        var parameters = string.Join(", ", beat.Parameters.Select(kv => $"{kv.Key}={kv.Value}"));
        var steps = string.Join(", ", beat.Steps);

        return $"[magenta]beat[/] [yellow]{Markup.Escape(beat.Name)}[/]" +
               $"[grey]({Markup.Escape(parameters)})[/] " +
               $"[grey]{{[/] [cyan]{Markup.Escape(steps)}[/] [grey]}}[/]";
    }

    private static string DescribePlay(PlayNode play)
    {
        var parameters = string.Join(", ", play.Parameters.Select(kv => $"{kv.Key}={kv.Value}"));

        return $"[magenta]play[/] [yellow]@{Markup.Escape(play.PatternName)}[/] " +
               $"[grey]({Markup.Escape(parameters)})[/]";
    }
}
