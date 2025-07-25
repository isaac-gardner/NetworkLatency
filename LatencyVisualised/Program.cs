using Shared;
using Spectre.Console;
using System.Text.Json;

var json = File.ReadAllText("C:\\src\\NetworkLatency\\NetworkLatency\\bin\\Debug\\net8.0\\ping_data.json");
List<PingData> dataPoints = JsonSerializer.Deserialize<List<PingData>>(json)!;

List<Outage> outages = GetOutagesWithDurations(dataPoints);

while (true)
{
    var avgServerLatency = dataPoints.Where(dp => dp.ServerLatencyMs.HasValue).Average(dp => dp.ServerLatencyMs!.Value); //calculate average 
    var maxSeverLatency = dataPoints.Where(dp => dp.ServerLatencyMs.HasValue).Max(dp => dp.ServerLatencyMs);
    var avgGwLatency = dataPoints.Where(dp => dp.GatewayLatency.HasValue).Average(dp => dp.GatewayLatency!.Value); //calculate average 
    var maxGwLatency = dataPoints.Where(dp => dp.GatewayLatency.HasValue).Max(dp => dp.GatewayLatency);
    var dropoutCount = dataPoints.Count(dp => dp.IsDropout);

    var statsText = new Markup($@"
[b]Average internet Latency:[/] {avgServerLatency:F1}ms  
[b]Average gateway Latency:[/] {avgGwLatency:F1}ms  
[b]Max internet Latency:[/] {maxSeverLatency}ms  
[b]Max gateway Latency:[/] {maxGwLatency}ms  
[b]Dropouts:[/] [red]{dropoutCount}[/]
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
    AnsiConsole.Write(new Panel(statsText).Header("Summary").BorderColor(Color.Blue));
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

