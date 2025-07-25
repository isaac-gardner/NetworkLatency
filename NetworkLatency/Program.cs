
using Newtonsoft.Json;
using Shared;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

string server = "8.8.8.8"; // Google DNS or another stable server
string localGateway = "192.168.178.1";

var pingDataList = new List<PingData>();
int consecutiveFailures = 0;
const int maxFailuresBeforeDropout = 3; // Consider dropout after 3 consecutive failures

// File to store JSON data
string filePath = "ping_data.json";


// Start collecting data every 1 second
Timer timer = new Timer(_ =>
{
    _ = CollectPingDataAsync(); // Fire-and-forget async call
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

Console.WriteLine("Press any key to stop...");
Console.ReadKey();

// Final serialize before exit
SerializeData(pingDataList, filePath);

// Define the async method
async Task CollectPingDataAsync()
{
    try
    {
        PingData data = await MeasurePingAsync(server, localGateway, new PingState(), new PingState(), maxFailuresBeforeDropout);
        pingDataList.Add(data);

        if (pingDataList.Count % 10 == 0)
        {
            SerializeData(pingDataList, filePath);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error collecting ping data: {ex.Message}");
    }
}



static async Task<PingData> MeasurePingAsync(string server, string localGateway, PingState serverPingState, PingState gatewayPingState, int maxFailuresBeforeDropout)
{
    PingData data = new PingData
    {
        Server = server,
        Gateway = localGateway,
        Timestamp = DateTime.Now
    };
    var pingServer = new Ping();
    var pingGateway = new Ping();

    Task<PingReply>? serverPingTask = null;
    Task<PingReply>? gatewayPingTask = null;

    try
    {
        serverPingTask = pingServer.SendPingAsync(server);
        gatewayPingTask = pingGateway.SendPingAsync(localGateway);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error starting ping tasks: {e.Message}");
        data.ServerLatencyMs = null;
        data.IsDropout = true;
        serverPingState.AddFailure();
        gatewayPingState.AddFailure();
        return data;
    }

    PingReply? serverPingReply = null;
    PingReply? gatewayPingReply = null;

    try
    {
        serverPingReply = await serverPingTask;
    }
    catch (Exception e)
    {
        Console.WriteLine($"Ping {server} failed: {e.Message}");
    }

    try
    {
        gatewayPingReply = await gatewayPingTask;
    }
    catch (Exception e)
    {
        Console.WriteLine($"Ping {localGateway} failed: {e.Message}");
    }


    if (serverPingReply?.Status == IPStatus.Success)
    {
        data.ServerLatencyMs = serverPingReply.RoundtripTime;
        data.IsDropout = false;
        serverPingState.Reset(); // Reset failures on success
    }
    else
    {
        // Handle other non-success status codes
        data.ServerLatencyMs = null;
        data.IsDropout = true;
        serverPingState.AddFailure();
    }

    if (gatewayPingReply?.Status == IPStatus.Success)
    {
        data.GatewayLatency = gatewayPingReply.RoundtripTime;
        data.IsDropout = false;
        gatewayPingState.Reset();
    }
    else
    {
        data.GatewayLatency = null;
        data.IsDropout = true;
        gatewayPingState.AddFailure();
    }

    if (gatewayPingReply?.Status == IPStatus.Success && serverPingReply?.Status != IPStatus.Success) 
        data.IsInternet = true;

    return data;
}

 

static void SerializeData(List<PingData> dataList, string filePath)
{
    try
    {
        // Serialize data to JSON
        string json = JsonConvert.SerializeObject(dataList, Formatting.Indented);
        File.WriteAllText(filePath, json);
        Console.WriteLine($"Serialized {dataList.Count} entries to {filePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error serializing data: {ex.Message}");
    }
}


class PingState
{
    private int _consecutiveFailures = 0;
    public int ConsecutiveFailures => _consecutiveFailures;

    public void AddFailure() => _consecutiveFailures++;

    public void Reset() => _consecutiveFailures = 0;
}
