using System;
using System.Threading;

namespace Telegraf {

	public delegate bool SamplerFunc(int sampleRate);


	public class SamplerDefault {
		static long _counter;

		public static bool ShouldSend(int rate) {
			if (rate == 1) {
				return true;
			}
			return (Environment.TickCount ^ Interlocked.Increment(ref _counter))%rate == 0;
		}
	}

}