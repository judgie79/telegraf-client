using System;
using System.Collections.Generic;

namespace Telegraf {

	public class NullSender : ISender {
		public void Send(InfluxPoint point) {
			
		}
	}

}