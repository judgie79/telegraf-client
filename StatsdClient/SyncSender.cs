using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Telegraf {



	public class SyncSender : ISender {
		readonly ITextUDP _udp;
		private readonly Dictionary<string, string> _tags;

		public SyncSender(ITextUDP udp, Dictionary<string, string> tags) {
			_udp = udp;
			_tags = tags;
		}

		public void Send(InfluxPoint point) {
			using (var writer = new StringWriter()) {
				point.Format(writer, _tags);
				_udp.Send(writer.ToString());
			}
		}
	}

}