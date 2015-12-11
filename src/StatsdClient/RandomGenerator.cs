using System;
using System.Threading;

namespace Telegraf {

	public delegate bool SamplerFunc(double sampleRate);


	public class SamplerDefault {
		static long _counter;

		public static bool ShouldSend(double sampleRate) {
			if (sampleRate.Equals(1)) {
				return true;
			}
			var rate = Convert.ToInt32(1/sampleRate);
			return (Environment.TickCount ^ Interlocked.Increment(ref _counter))%rate == 0;
		}
	}

}