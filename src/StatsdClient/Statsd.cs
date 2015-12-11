using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Telegraf {

	public enum IntegralMetric {
		Counter,
		Timer, // can be sampled
		Histogram,
		Meter
	}


	public class Statsd : IStatsd {
		readonly object _commandCollectionLock = new object();

		StopwatchFactory StopwatchFactory { get; set; }
		IStatsdUDP Udp { get; set; }
		SamplerFunc SamplerFunc { get; set; }

		readonly string _prefix;

		public List<string> Commands { get; private set; }

		static string MetricToUnit(IntegralMetric m) {
			switch (m) {
				case IntegralMetric.Counter:
					return "c";

				case IntegralMetric.Timer:
					return "ms";

				case IntegralMetric.Histogram:
					return "h";

				case IntegralMetric.Meter:
					return "m";
				default:
					throw new ArgumentOutOfRangeException("m");
			}
		}

		public Statsd(IStatsdUDP udp, SamplerFunc samplerFunc, StopwatchFactory stopwatchFactory,
			string prefix) {
			Commands = new List<string>();
			StopwatchFactory = stopwatchFactory;
			Udp = udp;
			SamplerFunc = samplerFunc;
			_prefix = prefix;
		}

		public Statsd(IStatsdUDP udp, SamplerFunc samplerFunc, StopwatchFactory stopwatchFactory)
			: this(udp, samplerFunc, stopwatchFactory, string.Empty) {}


		public Statsd(IStatsdUDP udp, SamplerFunc samplerFunc)
			: this(udp, samplerFunc, () => {
				var watch = Stopwatch.StartNew();
				return (() => (int) watch.ElapsedMilliseconds);
			}) {}


		public Statsd(IStatsdUDP udp, string prefix)
			: this(udp, SamplerDefault.ShouldSend, () => {
				var watch = Stopwatch.StartNew();
				return (() => (int) watch.ElapsedMilliseconds);
			}, prefix) {}

		public Statsd(IStatsdUDP udp)
			: this(udp, "") {}

		public void SendGauge(string name, double value, bool isDeltaValue) {
			if (isDeltaValue) {
				// Sending delta values to StatsD requires a value modifier sign (+ or -) which we append 
				// using this custom format with a different formatting rule for negative/positive and zero values
				// https://msdn.microsoft.com/en-us/library/0c899ak8.aspx#SectionSeparator
				const string deltaValueStringFormat = "{0:+#.###;-#.###;+0}";
				Commands = new List<string> {
					GetCommand(name, string.Format(CultureInfo.InvariantCulture,
						deltaValueStringFormat,
						value),
						"g", 1)
				};
				Send();
			} else {
				Commands = new List<string> {
					GetCommand(name, String.Format(CultureInfo.InvariantCulture, "{0:F15}", value), "g", 1)
				};
				Send();
			}
		}

		public void SendSet(string name, string value) {
			Commands = new List<string> {
				GetCommand(name, value.ToString(CultureInfo.InvariantCulture), "s", 1)
			};
			Send();
		}

		public void AddGauge(string name, double value) {
			ThreadSafeAddCommand(GetCommand(name,
				String.Format(CultureInfo.InvariantCulture, "{0:F15}", value), "g", 1));
		}

		public void SendInteger(IntegralMetric metric, string name, int value, double sampleRate = 1) {
			if (SamplerFunc(sampleRate)) {
				Commands = new List<string> {
					GetCommand(name, value.ToString(CultureInfo.InvariantCulture), MetricToUnit(metric), sampleRate)
				};
				Send();
			}
		}

		public void AddInteger(IntegralMetric metric, string name, int value, double sampleRate = 1) {
			if (SamplerFunc(sampleRate)) {
				Commands.Add(GetCommand(name, value.ToString(CultureInfo.InvariantCulture), MetricToUnit(metric),
					sampleRate));
			}
		}

		void ThreadSafeAddCommand(string command) {
			lock (_commandCollectionLock) {
				Commands.Add(command);
			}
		}

		public void Send() {
			try {
				Udp.Send(string.Join("\n", Commands.ToArray()));
				Commands = new List<string>();
			}
			catch (Exception e) {
				Debug.WriteLine(e.Message);
			}
		}

		string GetCommand(string name, string value, string unit, double sampleRate) {
			var format = sampleRate.Equals(1) ? "{0}:{1}|{2}" : "{0}:{1}|{2}|@{3}";
			return string.Format(CultureInfo.InvariantCulture, format, _prefix + name, value, unit,
				sampleRate);
		}
	}

}