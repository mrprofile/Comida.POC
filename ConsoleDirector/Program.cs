using Spectre.Console.Cli;
using Spectre.Console;
using k8s;
using k8s.Models;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var app = new CommandApp<ConsoleCommand>();

        return await app.RunAsync(args);
    }
}


public class ConsoleCommand : AsyncCommand<ConsoleCommand.ConsoleSettings>
{
    public class ConsoleSettings : CommandSettings
    {
        [CommandArgument(0, "<arg1>")]
        public string Arg1 { get; set; }

        [CommandArgument(1, "<arg2>")]
        public string Arg2 { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ConsoleSettings settings)
    {
        AnsiConsole
            .Write(new FigletText("Comida Director POC")
                .LeftJustified()
                .Color(Color.Red));

        AnsiConsole.WriteLine("You provided the following arguments:");
        AnsiConsole.WriteLine($"Argument 1:[/] {settings.Arg1}");
        AnsiConsole.WriteLine($"Argument 2:[/] {settings.Arg2}");

        var filePath = Directory.GetCurrentDirectory() + "//comida.console.yaml";
        var job = await KubernetesYaml.LoadFromFileAsync<V1Job>(filePath);

        /* Setup Metadata to setup Job */
        job.Metadata.Name = job.Metadata.Name + "-" + settings.Arg1 + "-" + Guid.NewGuid().ToString("N");
        job.Spec.Template.Spec.Containers[0].Args = new List<string>
        {
            settings.Arg1,
            settings.Arg2
        };

        /* Add Context to JOB from command arguments */
        job.Spec.Template.SetLabel("tenant", settings.Arg1);
        job.Spec.Template.SetLabel("tenant-id", settings.Arg2);

        /* Setup kubernetes client */
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(Directory.GetCurrentDirectory() + "//config");
        IKubernetes client = new Kubernetes(config);
        Console.WriteLine("Starting Request!");

        /* Create Job */
        var result = await client.BatchV1.CreateNamespacedJobAsync(job, "default");

        Console.WriteLine(result.Metadata.Name);

        /* Setup selector for querying individual POD in Kubernetes */
        var selector = result.Spec.Selector;
        var labelSelector = string.Join(",", selector.MatchLabels.Select(x => x.Key + "=" + x.Value).ToArray());

        var podlist = client.CoreV1.ListNamespacedPodWithHttpMessagesAsync("default", watch: true, labelSelector: labelSelector);
      
        // C# 8 required https://docs.microsoft.com/en-us/archive/msdn-magazine/2019/november/csharp-iterating-with-async-enumerables-in-csharp-8
        await foreach (var (type, item) in podlist.WatchAsync<V1Pod, V1PodList>())
        {
            Console.WriteLine("==on watch event==");
            Console.WriteLine(type);
            Console.WriteLine(item.Metadata.Name);
            Console.WriteLine("==on watch event==");

            if (type == WatchEventType.Deleted)
            {
                Console.WriteLine("==on watch event= DONE");

                // GET Context Data via Labels
                foreach(var label in item.Metadata.Labels)
                {
                    Console.WriteLine(label.Value + "=" + label.Value);
                }
                
                break;
            }
        }

        return 0;
    }
}