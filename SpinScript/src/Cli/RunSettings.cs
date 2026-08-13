namespace SpinScript.Cli;

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

public sealed class RunSettings : CommandSettings
{
    [CommandArgument(0, "[input]")]
    [Description("Caminho de um arquivo .spin ou código SpinScript direto entre aspas.")]
    public string? Input { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Input))
        {
            return ValidationResult.Error("Informe um arquivo .spin ou código SpinScript entre aspas. Use --help para ver exemplos.");
        }

        return ValidationResult.Success();
    }
}
