using System.Xml.Linq;
using MessagePack;
using Spectre.Console;
using YADIS.OhioT1DM.Parser.Logic;

namespace YADIS.OhioT1DM.Parser;

public static class Program
{
    public static void Main()
    {
        AnsiConsole.Write(new Rule("[cyan]OhioT1DM XML to MessagePack Converter[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var inputRoot = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold green]Input directory[/] (OhioT1DM XML root):")
                    .PromptStyle("yellow")
                    .Validate(path =>
                    {
                        var cleanPath = path.Trim().Trim('"');
                        if (string.IsNullOrWhiteSpace(cleanPath))
                            return ValidationResult.Error("[red]Path cannot be empty.[/]");

                        return Directory.Exists(cleanPath)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]Directory does not exist. Please check the path.[/]");
                    }))
            .Trim()
            .Trim('"')
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var defaultOutput = Path.Combine(
            Directory.GetParent(inputRoot)?.FullName ?? inputRoot,
            $"{Path.GetFileName(inputRoot)}_msgpack");

        var outputRoot = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold green]Output directory[/]:")
                    .DefaultValue(defaultOutput)
                    .DefaultValueStyle("grey")
                    .PromptStyle("yellow")
                    .Validate(path => string.IsNullOrWhiteSpace(path.Trim().Trim('"'))
                        ? ValidationResult.Error("[red]Output path cannot be empty.[/]")
                        : ValidationResult.Success()))
            .Trim()
            .Trim('"')
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Parameter[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Input Root", $"[cyan]{Markup.Escape(inputRoot)}[/]");
        table.AddRow("Output Root", $"[cyan]{Markup.Escape(outputRoot)}[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("Start processing?"))
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return;
        }

        ProcessDirectory(inputRoot, outputRoot);
    }

    private static void ProcessDirectory(string rootPath, string targetRoot)
    {
        var files = Directory.GetFiles(rootPath, "*.xml", SearchOption.AllDirectories);

        if (files.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No XML files found in the specified directory.[/]");
            return;
        }

        var parser = new OhioT1DmDatasetParser();

        var c = 0;

        AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(),
                new RemainingTimeColumn(), new SpinnerColumn())
            .Start(ctx =>
            {
                var task = ctx.AddTask("[green]Converting datasets[/]");
                task.MaxValue(files.Length);

                foreach (var file in files)
                {
                    c += ProcessFile(file, targetRoot, parser);
                    task.Increment(1);
                }
            });

        AnsiConsole.MarkupLine($"[bold green]Done![/] Converted {c} file(s).");
    }

    private static int ProcessFile(string filePath, string targetRoot, OhioT1DmDatasetParser parser)
    {
        try
        {
            var document = XDocument.Load(filePath);

            if (filePath.Contains("596-ws"))
                OhioT1DmDatasetParser.Fix596Document(document);

            var data = parser.Parse(document);

            var outDir = Path.Combine(targetRoot, GetSubFolderName(filePath));
            Directory.CreateDirectory(outDir);

            var outputPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(filePath) + ".mpk");
            File.WriteAllBytes(outputPath, MessagePackSerializer.Serialize(data));

            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[red]Error processing {Markup.Escape(Path.GetFileName(filePath))}:[/] {Markup.Escape(ex.Message)}");

            return 0;
        }
    }

    private static string GetSubFolderName(string filePath)
    {
        return Path.GetFileName(filePath).Contains("testing", StringComparison.OrdinalIgnoreCase)
            ? "testing"
            : "training";
    }
}