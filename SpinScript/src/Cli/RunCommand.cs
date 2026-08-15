namespace SpinScript.Cli;

using Spectre.Console;
using Spectre.Console.Cli;
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

        ProgramNode program;
        try
        {
            program = new Parser(source).Parse();
        }
        catch (LexerException ex)
        {
            WriteErrorPanel("Erro léxico", ex.Message);
            return 1;
        }
        catch (ParserException ex)
        {
            WriteErrorPanel("Erro de sintaxe", ex.Message);
            return 1;
        }

        WriteAst(program, sourceLabel);
        AnsiConsole.MarkupLine($"[green]✓[/] {program.Statements.Count} statement(s) reconhecido(s). [grey](ainda não há interpretador — apenas o parser está implementado)[/]");

        return 0;
    }

    private static (string source, string label, bool ok) LoadSource(string input)
    {
        if (input.EndsWith(".spin", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(input))
            {
                AnsiConsole.MarkupLine($"[red]Arquivo não encontrado:[/] {Markup.Escape(input)}");
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

            case PatternNode pattern:
                parent.AddNode(DescribePattern(pattern));
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

    private static string DescribePattern(PatternNode pattern)
    {
        var parameters = string.Join(", ", pattern.Parameters.Select(kv => $"{kv.Key}={kv.Value}"));
        var steps = string.Join(", ", pattern.Steps);

        return $"[magenta]pattern[/] [yellow]{Markup.Escape(pattern.Name)}[/]" +
               $"[grey]({Markup.Escape(parameters)})[/] " +
               $"[grey]{{[/] [cyan]{Markup.Escape(steps)}[/] [grey]}}[/]";
    }

    private static string DescribePlay(PlayNode play)
    {
        var parameters = string.Join(", ", play.Parameters.Select(kv => $"{kv.Key}={kv.Value}"));

        return $"[magenta]play[/] [yellow]@{Markup.Escape(play.PatternName)}[/] " +
               $"[grey]({Markup.Escape(parameters)})[/]";
    }

    private static void WriteErrorPanel(string title, string message)
    {
        var panel = new Panel(Markup.Escape(message))
        {
            Header = new PanelHeader($"[red bold]{title}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Red),
        };

        AnsiConsole.Write(panel);
    }
}
