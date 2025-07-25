// See https://aka.ms/new-console-template for more information
using Spectre.Console;
using System.Text.Json;




// Step 2: Read and parse the data
var json = File.ReadAllText("data.json");
List<DataPoint> dataPoints = JsonSerializer.Deserialize<List<DataPoint>>(json)!;

// Step 3: Create a Table
var table = new Table();
table.AddColumns("Time", "Latency", "Status");

foreach (var point in dataPoints)
{
    var time = point.Timestamp.ToString("HH:mm:ss");
    var latency = $"{point.LatencyMs}ms";
    var status = point.IsDropout ? "[red]DROPOUT[/]" : "[green]OK[/]";
    table.AddRow(time, latency, status);
}

AnsiConsole.Write(new Panel(table).Header("Latency Table").BorderColor(Color.Grey));

// Step 4: Create a Bar Chart
var chart = new BarChart()
    .Label("Latency Over Time")
    .CenterLabel()
    .Width(60);

foreach (var point in dataPoints)
{
    var label = point.Timestamp.ToString("HH:mm");
    var color = point.IsDropout ? Color.Red : Color.Green;
    chart.AddItem(label, point.LatencyMs, color);
}

AnsiConsole.Write(chart);

// Step 5: Summary Stats Panel
var avgLatency = dataPoints.Average(dp => dp.LatencyMs);
var maxLatency = dataPoints.Max(dp => dp.LatencyMs);
var dropoutCount = dataPoints.Count(dp => dp.IsDropout);

var statsText = new Markup($@"
[b]Average Latency:[/] {avgLatency:F1}ms  
[b]Max Latency:[/] {maxLatency}ms  
[b]Dropouts:[/] [red]{dropoutCount}[/]
");

AnsiConsole.Write(new Panel(statsText).Header("Summary").BorderColor(Color.Blue));


// Step 1: Define the model
class DataPoint
{
    public DateTime Timestamp { get; set; }
    public int LatencyMs { get; set; }
    public bool IsDropout { get; set; }
}