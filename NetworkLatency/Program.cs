
using Newtonsoft.Json;
using System.Net.NetworkInformation;

string server = "8.8.8.8"; // Google DNS or another stable server
        var ping = new Ping();
        var pingDataList = new List<PingData>();
        int consecutiveFailures = 0;
        const int maxFailuresBeforeDropout = 3; // Consider dropout after 3 consecutive failures

        // File to store JSON data
        string filePath = "ping_data.json";

        // Start collecting data every 1 second
        Timer timer = new Timer(_ =>
        {
            PingData data = MeasurePing(ping, server, ref consecutiveFailures, maxFailuresBeforeDropout);
            pingDataList.Add(data);

            // Optional: Serialize to file after every 10 data points
            if (pingDataList.Count % 10 == 0)
            {
                SerializeData(pingDataList, filePath);
            }

        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();

        // Final serialize before exit
        SerializeData(pingDataList, filePath);
 

    static PingData MeasurePing(Ping ping, string server, ref int consecutiveFailures, int maxFailuresBeforeDropout)
    {
        PingData data = new PingData
        {
            Timestamp = DateTime.Now
        };

        try
        {
            PingReply reply = ping.Send(server);
            if (reply.Status == IPStatus.Success)
            {
                data.LatencyMs = reply.RoundtripTime;
                data.IsDropout = false;
                consecutiveFailures = 0; // Reset failures on success
            }
            else
            {
                // Handle other non-success status codes
                data.LatencyMs = null;
                data.IsDropout = true;
                consecutiveFailures++;
            }
        }
        catch (Exception)
        {
            // Handle exceptions (e.g., no network)
            data.LatencyMs = null;
            data.IsDropout = true;
            consecutiveFailures++;
        }

        // If we reach the dropout threshold, consider it a true dropout (set IsDropout to true)
        if (consecutiveFailures >= maxFailuresBeforeDropout)
        {
            data.IsDropout = true;
        }

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

public class PingData
{
    public DateTime Timestamp { get; set; }
    public long? LatencyMs { get; set; }
    public bool IsDropout { get; set; }
}
