using System.Collections.Generic;

namespace Telegraf {

	public class MetricsConfig {
		/// <summary>
		/// The full host name of your statsd server.
		/// </summary>
		public string ServerName { get; set; }

		/// <summary>
		/// Uses the statsd default port if not specified (8125).
		/// </summary>
		public int ServerPort { get; set; }

		/// <summary>
		/// Allows you to override the maximum UDP packet size (in bytes) if your setup requires that. Defaults to 512.
		/// </summary>
		public int MaxUDPPacketSize { get; set; }

		/// <summary>
		/// Allows you to optionally specify a stat name prefix for all your stats.
		/// </summary>
		public Dictionary<string,string> Tags { get; set; }

		public const int DefaultCollectorPort = 8094;
		public const int DefaultMaxUDPPacketSize = 512;
		public const string DefaultServerName = "127.0.0.1";


		public int AsyncMaxNumberOfPointsInQueue { get; set; }
		public int AsyncPutXMetricsInUDPPacket { get; set; }

        public TextType TextType { get; set; }

        public MetricsConfig() {
			Tags = new Dictionary<string, string>();
			ServerPort = DefaultCollectorPort;
			MaxUDPPacketSize = DefaultMaxUDPPacketSize;
			ServerName = DefaultServerName;
			AsyncMaxNumberOfPointsInQueue = 10000;
			AsyncPutXMetricsInUDPPacket = 4;
            TextType = TextType.Udp;

        }
	}

    public enum TextType
    {
        Http,
        Udp
    }

}