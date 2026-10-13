namespace SpinScript.Cli;

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

public sealed class RunSettings : CommandSettings
{
    [CommandArgument(0, "[input]")]
    [Description("Path to a .spin file, or SpinScript source in quotes.")]
    public string? Input { get; set; }

    [CommandOption("--ast")]
    [Description("Also print the parsed syntax tree.")]
    public bool ShowAst { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Input))
        {
            return ValidationResult.Error("Pass a .spin file or SpinScript source in quotes. Use --help for examples.");
        }

        return ValidationResult.Success();
    }
}
