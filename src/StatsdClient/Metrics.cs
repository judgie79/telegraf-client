using System;
using System.Linq;
using System.Text;

namespace Telegraf {

	public static class Metrics {
		static IStatsd _statsD = new NullStatsd();
		static StatsdUDP _statsdUdp;
		static string _tags;

		/// <summary>
		/// Configures the Metric class with a configuration. Call this once at application startup (Main(), Global.asax, etc).
		/// </summary>
		/// <param name="config">Configuration settings.</param>
		public static void Configure(MetricsConfig config) {
			if (config == null) {
				throw new ArgumentNullException("config");
			}

			_tags = string.Join(",", config.Tags.ToArray());
			CreateStatsD(config);
		}

		static void CreateStatsD(MetricsConfig config) {
			if (_statsdUdp != null) {
				_statsdUdp.Dispose();
			}

			_statsdUdp = null;

			if (!string.IsNullOrEmpty(config.StatsdServerName)) {
				_statsdUdp = new StatsdUDP(config.StatsdServerName, config.StatsdServerPort,
					config.StatsdMaxUDPPacketSize);
				_statsD = new Statsd(_statsdUdp);
			}
		}


		/// <summary>
		/// Modify the current value of the gauge with the given value.
		/// </summary>
		/// <param name="statName">Name of the metric.</param>
		/// <param name="deltaValue"></param>
		public static void GaugeDelta(string statName, double deltaValue) {
			_statsD.SendGauge(BuildNamespacedStatName(statName), deltaValue, true);
		}

		/// <summary>
		/// Set the gauge to the given absolute value.
		/// </summary>
		/// <param name="statName">Name of the metric.</param>
		/// <param name="absoluteValue">Absolute value of the gauge to set.</param>
		public static void GaugeAbsoluteValue(string statName, double absoluteValue, string[] tags = null) {
			_statsD.SendGauge(BuildNamespacedStatName(statName, tags), absoluteValue, false);
		}

		[Obsolete(
			"Will be removed in future version. Use explicit GaugeDelta or GaugeAbsoluteValue instead.")]
		public static void Gauge(string statName, double value) {
			GaugeAbsoluteValue(statName, value);
		}

		/// <summary>
		/// Send a counter value.
		/// </summary>
		/// <param name="statName">Name of the metric.</param>
		/// <param name="value">Value of the counter. Defaults to 1.</param>
		/// <param name="sampleRate">Sample rate to reduce the load on your metric server. Defaults to 1 (100%).</param>
		public static void Counter(string statName, int value = 1, double sampleRate = 1,
			string[] tags = null) {
			_statsD.SendInteger(IntegralMetric.Counter, BuildNamespacedStatName(statName, tags), value,
				sampleRate);
		}

		/// <summary>
		/// Send a manually timed value.
		/// </summary>
		/// <param name="statName">Name of the metric.</param>
		/// <param name="value">Elapsed miliseconds of the event.</param>
		/// <param name="sampleRate">Sample rate to reduce the load on your metric server. Defaults to 1 (100%).</param>
		public static void Timer(string statName, int value, double sampleRate = 1, string[] tags = null) {
			_statsD.SendInteger(IntegralMetric.Timer, BuildNamespacedStatName(statName, tags), value,
				sampleRate);
		}


		/// <summary>
		/// Store a unique occurence of an event between flushes.
		/// </summary>
		/// <param name="statName">Name of the metric.</param>
		/// <param name="value">Value to set.</param>
		public static void Set(string statName, string value, string[] tags = null) {
			_statsD.SendSet(BuildNamespacedStatName(statName, tags), value);
		}

		static string BuildNamespacedStatName(string statName, params string[] tags) {
			var builder = new StringBuilder(statName);
			if (!string.IsNullOrEmpty(_tags)) {
				builder.Append(',').Append(_tags);
			}
			if (tags != null) {
				for (int i = 0; i < tags.Length; i++) {
					builder.Append(',').Append(tags[i]);
				}
			}
			return builder.ToString();
		}
	}

}