using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Telegraf {



	public class SyncSender : ISender {
		readonly IText _text;
		private readonly Dictionary<string, string> _tags;

		public SyncSender(IText udp, Dictionary<string, string> tags) {
			_text = udp;
			_tags = tags;
		}

		public void Send(InfluxPoint point) {
			using (var writer = new StringWriter()) {
				point.Format(writer, _tags);
				_text.Send(writer.ToString());
			}
		}
	}

}