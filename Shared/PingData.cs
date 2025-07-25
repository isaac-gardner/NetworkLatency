using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class PingData
    {
        public DateTime Timestamp { get; set; }

        public string Hostname { get; set; } = Dns.GetHostName();

        public string Server { get; set; } = "";
        public long? ServerLatencyMs { get; set; }

        public string Gateway { get; set; } = "";
        public long? GatewayLatency { get; set; }
        public bool IsDropout { get; set; } = false;

        public bool IsInternet { get; set; } = false;
    }

}
