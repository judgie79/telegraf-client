using System;
using System.Collections.Generic;

namespace Telegraf {

	public interface ISender {
		void Send(InfluxPoint point);
	}

}