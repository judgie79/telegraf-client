﻿using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rhino.Mocks;
using Telegraf;

namespace Tests {

	[TestFixture]
	public class StatsdTests {
		IStatsdUDP _udp;
		//private SamplerFunc _samplerFunc;


		[SetUp]
		public void Setup() {
			_udp = MockRepository.GenerateMock<IStatsdUDP>();
			//  _samplerFunc = MockRepository.GenerateMock<SamplerFunc>();
			//_samplerFunc.Stub(x => x.ShouldSend(Arg<double>.Is.Anything)).Return(true);
		}

		public static bool SampleEverything(double rate) {
			return true;
		}

		public static bool SampleNothing(double rate) {
			return false;
		}

		static StopwatchMeasurement MeasureHalfASecond() {
			return (() => 500);
		}

		public class Counter : StatsdTests {
			[Test]
			public void increases_counter_with_value_of_X() {
				var s = new Statsd(_udp, SampleEverything);
				s.SendInteger(IntegralMetric.Counter, "counter", 5);
				_udp.AssertWasCalled(x => x.Send("counter:5|c"));
			}

			[Test]
			public void increases_counter_with_value_of_X_and_sample_rate() {
				var s = new Statsd(_udp, SampleEverything);
				s.SendInteger(IntegralMetric.Counter, "counter", 5, 0.1);
				_udp.AssertWasCalled(x => x.Send("counter:5|c|@0.1"));
			}

			[Test]
			public void counting_exception_fails_silently() {
				var s = new Statsd(_udp, SampleEverything);
				_udp.Stub(x => x.Send(Arg<string>.Is.Anything)).Throw(new Exception());
				s.SendInteger(IntegralMetric.Counter, "counter", 5);
				Assert.Pass();
			}
		}

		public class Timer : StatsdTests {
			[Test]
			public void adds_timing() {
				var s = new Statsd(_udp, SampleEverything);
				s.SendInteger(IntegralMetric.Timer, "timer", 5);
				_udp.AssertWasCalled(x => x.Send("timer:5|ms"));
			}

			[Test]
			public void timing_with_value_of_X_and_sample_rate() {
				var s = new Statsd(_udp, SampleEverything);
				s.SendInteger(IntegralMetric.Timer, "timer", 5, 0.1);
				_udp.AssertWasCalled(x => x.Send("timer:5|ms|@0.1"));
			}

			[Test]
			public void timing_exception_fails_silently() {
				_udp.Stub(x => x.Send(Arg<string>.Is.Anything)).Throw(new Exception());
				var s = new Statsd(_udp);
				s.SendInteger(IntegralMetric.Timer, "timer", 5);
				Assert.Pass();
			}
		}

		public class Guage : StatsdTests {
			[Test]
			public void adds_gauge_with_large_double_values() {
				var s = new Statsd(_udp, SampleEverything);
				s.SendGauge("gauge", 34563478564785, false);
				_udp.AssertWasCalled(x => x.Send("gauge:34563478564785.000000000000000|g"));
			}

			[Test]
			public void gauge_exception_fails_silently() {
				_udp.Stub(x => x.Send(Arg<string>.Is.Anything)).Throw(new Exception());
				var s = new Statsd(_udp);
				s.SendGauge("gauge", 5.0, false);
				Assert.Pass();
			}

			[Test]
			[TestCase(true, 10d, "delta-gauge:+10|g")]
			[TestCase(true, -10d, "delta-gauge:-10|g")]
			[TestCase(true, 0d, "delta-gauge:+0|g")]
			[TestCase(false, 10d, "delta-gauge:10.000000000000000|g")]
			//because it is looped through to original Gauge send function
			public void adds_gauge_with_deltaValue_formatsCorrectly(bool isDeltaValue, double value,
				string expectedFormattedStatsdMessage) {
				var s = new Statsd(_udp, SampleEverything);
				s.SendGauge("delta-gauge", value, isDeltaValue);
				_udp.AssertWasCalled(x => x.Send(expectedFormattedStatsdMessage));
			}
		}

		public class Meter : StatsdTests {
			[Test]
			public void adds_meter() {
				var s = new Statsd(_udp, SampleEverything);
				s.SendInteger(IntegralMetric.Meter, "meter", 5);
				_udp.AssertWasCalled(x => x.Send("meter:5|m"));
			}

			[Test]
			public void meter_exception_fails_silently() {
				_udp.Stub(x => x.Send(Arg<string>.Is.Anything)).Throw(new Exception());
				var s = new Statsd(_udp);
				s.SendInteger(IntegralMetric.Meter, "meter", 5);
				Assert.Pass();
			}
		}

		public class Historgram : StatsdTests {
			[Test]
			public void adds_histogram() {
				var s = new Statsd(_udp, SampleEverything);
				s.SendInteger(IntegralMetric.Histogram, "histogram", 5);
				_udp.AssertWasCalled(x => x.Send("histogram:5|h"));
			}

			[Test]
			public void histrogram_exception_fails_silently() {
				_udp.Stub(x => x.Send(Arg<string>.Is.Anything)).Throw(new Exception());
				var s = new Statsd(_udp);
				s.SendInteger(IntegralMetric.Histogram, "histogram", 5);
				Assert.Pass();
			}
		}

		public class Set : StatsdTests {
			[Test]
			public void adds_set_with_string_value() {
				var s = new Statsd(_udp, SampleEverything);
				s.SendSet("set", "34563478564785xyz");
				_udp.AssertWasCalled(x => x.Send("set:34563478564785xyz|s"));
			}

			[Test]
			public void set_exception_fails_silently() {
				_udp.Stub(x => x.Send(Arg<string>.Is.Anything)).Throw(new Exception());
				var s = new Statsd(_udp);
				s.SendSet("set", "silent-exception-test");
				Assert.Pass();
			}
		}

		public class Combination : StatsdTests {
			[Test]
			public void add_one_counter_and_one_gauge_shows_in_commands() {
				var s = new Statsd(_udp, SampleEverything);
				s.AddInteger(IntegralMetric.Counter, "counter", 1, 0.1);
				s.AddInteger(IntegralMetric.Timer, "timer", 1);

				Assert.That(s.Commands.Count, Is.EqualTo(2));
				Assert.That(s.Commands[0], Is.EqualTo("counter:1|c|@0.1"));
				Assert.That(s.Commands[1], Is.EqualTo("timer:1|ms"));
			}

			[Test]
			public void add_one_counter_and_one_gauge_with_no_sample_rate_shows_in_commands() {
				var s = new Statsd(_udp, SampleEverything);
				s.AddInteger(IntegralMetric.Counter, "counter", 1);
				s.AddInteger(IntegralMetric.Timer, "timer", 1);

				Assert.That(s.Commands.Count, Is.EqualTo(2));
				Assert.That(s.Commands[0], Is.EqualTo("counter:1|c"));
				Assert.That(s.Commands[1], Is.EqualTo("timer:1|ms"));
			}

			[Test]
			public void add_one_counter_and_one_timer_sends_in_one_go() {
				var s = new Statsd(_udp, SampleEverything);
				s.AddInteger(IntegralMetric.Counter, "counter", 1, 0.1);
				s.AddInteger(IntegralMetric.Timer, "timer", 1);
				s.Send();

				_udp.AssertWasCalled(x => x.Send("counter:1|c|@0.1\ntimer:1|ms"));
			}

			[Test]
			public void add_one_counter_and_one_timer_sends_and_removes_commands() {
				var s = new Statsd(_udp, SampleEverything);
				s.AddInteger(IntegralMetric.Counter, "counter", 1, 0.1);
				s.AddInteger(IntegralMetric.Timer, "timer", 1);
				s.Send();

				Assert.That(s.Commands.Count, Is.EqualTo(0));
			}

			[Test]
			public void add_one_counter_and_send_one_timer_sends_only_sends_the_last() {
				var s = new Statsd(_udp, SampleEverything);
				s.AddInteger(IntegralMetric.Counter, "counter", 1);
				s.SendInteger(IntegralMetric.Timer, "timer", 1);

				_udp.AssertWasCalled(x => x.Send("timer:1|ms"));
			}
		}

		public class NamePrefixing : StatsdTests {
			[Test]
			public void set_prefix_on_stats_name_when_calling_send() {
				var s = new Statsd(_udp, "a.prefix.");
				s.SendInteger(IntegralMetric.Counter, "counter", 5);
				s.SendInteger(IntegralMetric.Counter, "counter", 5);

				_udp.AssertWasCalled(x => x.Send("a.prefix.counter:5|c"), x => x.Repeat.Twice());
			}

			[Test]
			public void add_counter_sets_prefix_on_name() {
				var s = new Statsd(_udp, SampleEverything, MeasureHalfASecond, "another.prefix.");

				s.AddInteger(IntegralMetric.Counter, "counter", 1, 0.1);
				s.AddInteger(IntegralMetric.Timer, "timer", 1);
				s.Send();

				_udp.AssertWasCalled(x => x.Send("another.prefix.counter:1|c|@0.1\nanother.prefix.timer:1|ms"));
			}
		}

		public class Concurrency : StatsdTests {
			[Test]
			public void can_concurrently_add_integer_metrics() {
				var s = new Statsd(_udp, SampleEverything);

				Parallel.For(0, 1000000,
					x => Assert.DoesNotThrow(() => s.AddInteger(IntegralMetric.Counter, "name", 5)));
			}

			[Test]
			public void can_concurrently_add_double_metrics() {
				var s = new Statsd(_udp, SampleEverything);

				Parallel.For(0, 1000000, x => Assert.DoesNotThrow(() => s.AddGauge("name", 5d)));
			}
		}

		static int TestMethod() {
			return 5;
		}
	}

}