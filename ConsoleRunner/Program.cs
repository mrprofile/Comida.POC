using Spectre.Console.Cli;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;

var app = new CommandApp<ConsoleCommand>();
return app.Run(args);


public class ConsoleCommand : Command<ConsoleCommand.ConsoleSettings>
{
    public class ConsoleSettings : CommandSettings
    {
        [CommandArgument(0, "<arg1>")]
        public string Arg1 { get; set; }

        [CommandArgument(1, "<arg2>")]
        public string Arg2 { get; set; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] ConsoleSettings settings)
    {
        AnsiConsole
            .Write(new FigletText("Comida POC")
                .LeftJustified()
                .Color(Color.Red));
        
        AnsiConsole.WriteLine("Sleeping for 30 seconds");
        Thread.Sleep(30000);
        AnsiConsole.WriteLine("Done");

        AnsiConsole.WriteLine("You provided the following arguments:");
        AnsiConsole.WriteLine($"Argument 1:[/] {settings.Arg1}");
        AnsiConsole.WriteLine($"Argument 2:[/] {settings.Arg2}");

        return 0;
    }
}