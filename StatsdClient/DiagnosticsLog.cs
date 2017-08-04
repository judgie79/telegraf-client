using System;
using System.IO;

namespace Telegraf {

	public static class DiagnosticsLog
	{
		public static TextWriter Out { get; set; }

		static DiagnosticsLog() {
			Out = Console.Error;
		}

		public static void WriteLine(string format, params object[] args)
		{
			var writer = Out;
			if (writer != null)
				writer.WriteLine(format, args);
		}
	}

}