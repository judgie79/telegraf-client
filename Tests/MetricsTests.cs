﻿using System;
using NUnit.Framework;
using Telegraf;

namespace Tests {

	public class MetricsTests {
		[Test]
		public void throws_when_configured_with_a_null_configuration() {
			Assert.Throws<ArgumentNullException>(() => Metrics.Configure(null));
		}
	}

}