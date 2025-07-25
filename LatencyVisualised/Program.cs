using Shared;
using Spectre.Console;
using System.Runtime.CompilerServices;
using System.Text.Json;

var filname = "C:\\src\\NetworkLatency\\NetworkLatency\\bin\\Debug\\net8.0\\ping_data.json";

AnsiConsole.Progress()
     .Start(pctx =>
     {
         // Add a task to track progress
         var task = pctx.AddTask("[green]Loading data...[/]");

         // Simulate work and check for error condition
         while (!File.Exists(filname))
         {
             // Increment the progress bar by 5
             task.Increment(5);

             // Simulate work with delay
             Thread.Sleep(1000);

 
         }
     });

while (true)
{
    List<PingData> dataPoints = new List<PingData>();
    try
    {
        var json = File.ReadAllText(filname);
        dataPoints = JsonSerializer.Deserialize<List<PingData>>(json)!;
    }
    catch (Exception ex)
    {
        AnsiConsole.Markup("[bold red]Error:[/] " + ex.Message);
        break;
    }

    List<Outage> outages = GetOutagesWithDurations(dataPoints);

    var avgServerLatency = dataPoints.Where(dp => dp.ServerLatencyMs.HasValue).Average(dp => dp.ServerLatencyMs!.Value); //calculate average 

    var maxSeverLatency = dataPoints.Where(dp => dp.ServerLatencyMs.HasValue).Max(dp => dp.ServerLatencyMs);
    var lastServerLatency = dataPoints.Where(dp => dp.ServerLatencyMs.HasValue).Last().ServerLatencyMs!.Value;
    var internetDropout = dataPoints.Count(dp => dp.IsInternet);

    var avgGwLatency = dataPoints.Where(dp => dp.GatewayLatency.HasValue).Average(dp => dp.GatewayLatency!.Value); //calculate average 
    var maxGwLatency = dataPoints.Where(dp => dp.GatewayLatency.HasValue).Max(dp => dp.GatewayLatency);
    var lastGwLatency = dataPoints.Where(dp => dp.GatewayLatency.HasValue).Last().GatewayLatency!.Value;
    var gwDropout = dataPoints.Count(db => db.IsDropout);


    var internet = new Markup($@"
[b]Average internet Latency:[/] {avgServerLatency:F1}ms  
[b]Max internet Latency:[/] [{(maxSeverLatency > 70 ? "yellow" :"white")}]{maxSeverLatency}ms[/] 
[b]Last internet Latency:[/] [{(lastServerLatency > 100 ?"red" : "white")}]{lastServerLatency}ms[/] 

[b]Dropouts:[/] [{(internetDropout > 0 ? "white":"white")}]{internetDropout}[/]
");

    var gw = new Markup($@"
[b]Average gateway Latency:[/] {avgGwLatency:F1}ms  
[b]Max gateway Latency:[/] [{(maxGwLatency > 20 ? "orange" : "white")}]{maxGwLatency}ms[/]
[b]Last gateway Latency:[/] [{(lastGwLatency > 50 ? "red":"white")}]{lastGwLatency}ms[/]  
");

    var table = new Table();
    table.AddColumn("Start Time");
    table.AddColumn("End Time");
    table.AddColumn("Duration (ms)");

    foreach (var outage in outages)
    {
        table.AddRow(outage.StartTime.ToString("HH:mm:ss"), outage.EndTime.ToString("HH:mm:ss"), $"{outage.DurationMs}ms");
    }

    AnsiConsole.Clear();  // Clear the screen before re-drawing
    AnsiConsole.Write(new Panel(internet).Header("Internet").BorderColor(Color.Blue));
    AnsiConsole.Write(table);

    await Task.Delay(8000);
}
// Method to calculate dropouts
List<Outage> GetOutagesWithDurations(List<PingData> dataPoints)
{
    List<Outage> outages = new List<Outage>();
    DateTime? outageStart = null;

    foreach (var dp in dataPoints)
    {
        if (dp.IsDropout)
        {
            if (outageStart == null)
                outageStart = dp.Timestamp;  // Start of a new dropout
        }
        else if (outageStart.HasValue)
        {
            // End of the dropout sequence
            outages.Add(new Outage
            {
                StartTime = outageStart.Value,
                EndTime = dp.Timestamp,
                DurationMs = (int)(dp.Timestamp - outageStart.Value).TotalMilliseconds
            });

            // Reset the outage start
            outageStart = null;
        }
    }

    // If the last data point ends with a dropout, add it as an outage
    if (outageStart.HasValue)
    {
        outages.Add(new Outage
        {
            StartTime = outageStart.Value,
            EndTime = dataPoints.Last().Timestamp,
            DurationMs = (int)(dataPoints.Last().Timestamp - outageStart.Value).TotalMilliseconds
        });
    }

    return outages;
}

class Outage
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMs { get; set; }
}

