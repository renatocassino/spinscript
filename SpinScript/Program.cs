using Spectre.Console.Cli;
using SpinScript.Cli;

var app = new CommandApp<RunCommand>();

app.Configure(config =>
{
    config.SetApplicationName("spinscript");
    config.SetApplicationVersion("0.1.0");

    config.AddExample("song.spin");
    config.AddExample("\"@bpm = 120;\"");
});

return app.Run(args);
