using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Telegraf;
using Tests.Helpers;

namespace Tests {

	[TestFixture]
	public class MetricIntegrationTests {
		UdpListener _udpListener;
		Thread _listenThread;
		const int _randomUnusedLocalPort = 23483;
		const string _localhostAddress = "127.0.0.1";
		MetricsConfig _defaultMetricsConfig;

		[TestFixtureSetUp]
		public void SetUpUdpListener() {
			_udpListener = new UdpListener(_localhostAddress, _randomUnusedLocalPort);
		}

		[TestFixtureTearDown]
		public void TearDownUdpListener() {
			_udpListener.Dispose();
		}

		[SetUp]
		public void StartUdpListenerThread() {
			_defaultMetricsConfig = new MetricsConfig {
				StatsdServerName = _localhostAddress,
				StatsdServerPort = _randomUnusedLocalPort
			};

			_listenThread = new Thread(_udpListener.Listen);
			_listenThread.Start();
		}

		string LastPacketMessageReceived() {
			// Stall until the the listener receives a message or times out.
			while (_listenThread.IsAlive) {}

			var lastMessages = _udpListener.GetAndClearLastMessages();
			try {
				return lastMessages[0];
			}
			catch (ArgumentOutOfRangeException) {
				return null;
			}
		}

		public class SanityCheck : MetricIntegrationTests {
			[Test]
			public void udp_listener_works() {
				var client = new StatsdUDP(_localhostAddress, _randomUnusedLocalPort);
				client.Send("iamnotinsane!");

				Assert.That(LastPacketMessageReceived(), Is.EqualTo("iamnotinsane!"));
			}
		}

		public class Counter : MetricIntegrationTests {
			[Test]
			public void counter() {
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Counter("counter");
				Assert.That(LastPacketMessageReceived(), Is.EqualTo("counter:1|c"));
			}

			[Test]
			public void counter_with_value() {
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Counter("counter", 10);
				Assert.That(LastPacketMessageReceived(), Is.EqualTo("counter:10|c"));
			}

			[Test]
			public void counter_with_default_tag() {
				_defaultMetricsConfig.Tags = new[] {"config_tag=value1"};
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Counter("counter");
				Assert.That(LastPacketMessageReceived(), Is.EqualTo("counter,config_tag=value1:1|c"));
			}

			[Test]
			public void counter_with_default_tag_and_value() {
				_defaultMetricsConfig.Tags = new[] {"config_tag=value1"};
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Counter("counter", tags: new[] {"local_tag=value2"});
				Assert.That(LastPacketMessageReceived(),
					Is.EqualTo("counter,config_tag=value1,local_tag=value2:1|c"));
			}

			[Test]
			public void counter_with_value_and_sampleRate() {
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Counter("counter", 10, 0.9999);
				Assert.That(LastPacketMessageReceived(), Is.EqualTo("counter:10|c|@0.9999"));
			}

			[Test]
			public void counter_with_no_config_setup_should_not_send_metric() {
				Metrics.Configure(new MetricsConfig());

				Metrics.Counter("counter");
				Assert.That(LastPacketMessageReceived(), Is.Null);
			}
		}

		public class Timer : MetricIntegrationTests {
			[Test]
			public void timer() {
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Timer("timer", 6);
				Assert.That(LastPacketMessageReceived(), Is.EqualTo("timer:6|ms"));
			}

			[Test]
			public void timer_with_tag() {
				_defaultMetricsConfig.Tags = new[] {"config_tag=value1"};
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Timer("timer", 6);
				Assert.That(LastPacketMessageReceived(), Is.EqualTo("timer,config_tag=value1:6|ms"));
			}

			[Test]
			public void timer_with_config_and_local_tags() {
				_defaultMetricsConfig.Tags = new[] {"config_tag=value1"};
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Timer("timer", 6, tags: new[] {"local_tag=value2"});
				Assert.That(LastPacketMessageReceived(),
					Is.EqualTo("timer,config_tag=value1,local_tag=value2:6|ms"));
			}

			[Test]
			public void timer_with_no_config_setup_should_not_send_metric() {
				Metrics.Configure(new MetricsConfig());

				Metrics.Timer("timer", 6);
				Assert.That(LastPacketMessageReceived(), Is.Null);
			}
		}


		public class GaugeDelta : MetricIntegrationTests {
			[Test]
			[TestCase(123d, "gauge:+123|g")]
			[TestCase(-123d, "gauge:-123|g")]
			[TestCase(0d, "gauge:+0|g")]
			public void GaugeDelta_EmitsCorrect_Format(double gaugeDeltaValue,
				string expectedPacketMessageFormat) {
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.GaugeDelta("gauge", gaugeDeltaValue);
				Assert.That(LastPacketMessageReceived(), Is.EqualTo(expectedPacketMessageFormat));
			}
		}

		public class GaugeObsolete : MetricIntegrationTests {
			[Test]
			public void obsolete_gauge_with_double_value() {
				Metrics.Configure(_defaultMetricsConfig);

				const double value = 12345678901234567890;
				Metrics.Gauge("gauge", value);
				Assert.That(LastPacketMessageReceived(),
					Is.EqualTo("gauge:12345678901234600000.000000000000000|g"));
			}
		}

		public class GaugeAbsolute : MetricIntegrationTests {
			[Test]
			public void absolute_gauge_with_double_value() {
				Metrics.Configure(_defaultMetricsConfig);

				const double value = 12345678901234567890;
				Metrics.GaugeAbsoluteValue("gauge", value);
				Assert.That(LastPacketMessageReceived(),
					Is.EqualTo("gauge:12345678901234600000.000000000000000|g"));
			}

			[Test]
			public void absolute_gauge_with_double_value_with_floating_point() {
				Metrics.Configure(_defaultMetricsConfig);

				const double value = 1.234567890123456;
				Metrics.GaugeAbsoluteValue("gauge", value);
				Assert.That(LastPacketMessageReceived(), Is.EqualTo("gauge:1.234567890123460|g"));
			}

			[Test]
			public void absolute_gauge_with_config_tag() {
				_defaultMetricsConfig.Tags = new[] {"config_tag=value1"};
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.GaugeAbsoluteValue("gauge", 3);
				Assert.That(LastPacketMessageReceived(),
					Is.EqualTo("gauge,config_tag=value1:3.000000000000000|g"));
			}

			[Test]
			public void absolute_gauge_with_config_and_local_tag() {
				_defaultMetricsConfig.Tags = new[] {"config_tag=value1"};
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.GaugeAbsoluteValue("gauge", 3, tags: new[] {"local_tag=value2"});
				Assert.That(LastPacketMessageReceived(),
					Is.EqualTo("gauge,config_tag=value1,local_tag=value2:3.000000000000000|g"));
			}

			[Test]
			public void gauge_with_no_config_setup_should_not_send_metric() {
				Metrics.Configure(new MetricsConfig());

				Metrics.GaugeAbsoluteValue("gauge", 3);
				Assert.That(LastPacketMessageReceived(), Is.Null);
			}
		}

		public class Set : MetricIntegrationTests {
			[Test]
			public void set() {
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Set("timer", "value");
				Assert.That(LastPacketMessageReceived(), Is.EqualTo("timer:value|s"));
			}

			[Test]
			public void set_with_config_tag() {
				_defaultMetricsConfig.Tags = new[] {"config_tag=value1"};
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Set("timer", "value");
				Assert.That(LastPacketMessageReceived(), Is.EqualTo("timer,config_tag=value1:value|s"));
			}

			[Test]
			public void set_with_config_and_local_tag() {
				_defaultMetricsConfig.Tags = new[] {"config_tag=value1"};
				Metrics.Configure(_defaultMetricsConfig);

				Metrics.Set("timer", "value", tags: new[] {"local_tag=value2"});
				Assert.That(LastPacketMessageReceived(),
					Is.EqualTo("timer,config_tag=value1,local_tag=value2:value|s"));
			}

			[Test]
			public void set_with_no_config_setup_should_not_send_metric() {
				Metrics.Configure(new MetricsConfig());

				Metrics.Set("timer", "value");
				Assert.That(LastPacketMessageReceived(), Is.Null);
			}
		}
	}

}